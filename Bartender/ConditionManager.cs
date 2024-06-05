using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bartender;

public interface IDisplayPriority
{
    public int DisplayPriority { get; }
}

public interface ICondition : IDisplayPriority
{
    public string ID { get; }
    public string ConditionName { get; }
    public bool Check(dynamic arg);
}

public interface IDrawableCondition
{
    public string GetTooltip(CondConfig cndCfg);
    public string GetSelectableTooltip(CondConfig cndCfg);
    public void Draw(CondConfig cndCfg);
}

public interface IArgCondition
{
    public dynamic GetDefaultArg(CondConfig cndCfg);
}

public interface IOnImportCondition
{
    public void OnImport(CondConfig cfg);
}

public interface IConditionCategory : IDisplayPriority
{
    public string CategoryName { get; }
}

public static class ConditionManager
{
    public const string ConstID = "Cond";

    private static readonly Dictionary<string, ICondition> Conditions = [];
    private static readonly Dictionary<ICondition, IConditionCategory> CategoryMap = [];
    private static readonly Dictionary<(ICondition, dynamic), bool> ConditionCache = [];
    private static readonly Dictionary<CondSetConfig, (bool previous, float time)> ConditionSetCache = [];
    private static readonly Dictionary<CondSetConfig, List<bool>> DebugSteps = [];
    private static readonly HashSet<CondSetConfig> LockedSets = [];
    private static float LastConditionCache = 0;

    public static List<(IConditionCategory category, List<ICondition> conditions)> ConditionCategories { get; private set; } = [];

    public enum BinaryOperator
    {
        AND,
        OR,
        EQUAL,
        XOR
    }

    public static void Initialize()
    {
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(IConditionCategory)) && !t.IsInterface))
        {
            var category = Activator.CreateInstance(t) as IConditionCategory;
            if (category == null) continue;

            var list = new List<ICondition>();
            ConditionCategories.Add((category, list));
            if (!t.IsAssignableTo(typeof(ICondition))) continue;

            list.Add((ICondition)category);
        }

        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(ICondition)) && !t.IsInterface))
        {
            var condition = Activator.CreateInstance(t) as ICondition;
            if (condition == null) continue;

            Conditions.Add(condition.ID, condition);

            var categoryType = t.GetCustomAttributes().FirstOrDefault(attr => attr.GetType().IsAssignableTo(typeof(IConditionCategory)))?.GetType();
            if (categoryType == null)
            {
                if (t.IsAssignableTo(typeof(IConditionCategory)))
                    CategoryMap.Add(condition, (IConditionCategory)condition);
                continue;
            }

            var (category, list) = ConditionCategories.FirstOrDefault(tuple => tuple.category.GetType() == categoryType);
            if (category == null) continue;

            list.Add(condition);
            CategoryMap.Add(condition, category);
        }

        ConditionCategories = ConditionCategories.OrderBy(t => t.category.DisplayPriority).ToList();
        for (int i = 0; i < ConditionCategories.Count; i++)
        {
            var (category, list) = ConditionCategories[i];
            ConditionCategories[i] = (category, list.OrderBy(c => c.DisplayPriority).ToList());
        }
    }

    public static ICondition GetCondition(string id) => Conditions.TryGetValue(id, out var condition) ? condition : null;

    public static IConditionCategory GetConditionCategory(ICondition condition) => CategoryMap[condition];
    public static IConditionCategory GetConditionCategory(string id) => GetConditionCategory(GetCondition(id));

    public static bool CheckCondition(string id, dynamic arg = null, bool negate = false)
    {
        var condition = GetCondition(id);
        return condition != null && (!negate ? CheckCondition(condition, arg) : !CheckCondition(condition, arg));
    }

    public static bool CheckCondition(ICondition condition, dynamic arg)
    {
        if (ConditionCache.TryGetValue((condition, arg), out bool cache))
            return cache;

        try
        {
            cache = condition.Check(arg);
        }
        catch
        {
            cache = false;
        }

        ConditionCache[(condition, arg)] = cache;
        return cache;
    }

    public static bool CheckUnaryCondition(bool negate, ICondition condition, dynamic arg)
    {
        try
        {
            return !negate ? condition.Check(arg) : !condition.Check(arg);
        }
        catch
        {
            return false;
        }
    }

    public static bool CheckBinaryCondition(bool previous, BinaryOperator op, bool negate, ICondition condition, dynamic arg)
    {
        return op switch
        {
            BinaryOperator.AND => previous && CheckUnaryCondition(negate, condition, arg),
            BinaryOperator.OR => previous || CheckUnaryCondition(negate, condition, arg),
            BinaryOperator.EQUAL => previous == CheckUnaryCondition(negate, condition, arg),
            BinaryOperator.XOR => previous ^ CheckUnaryCondition(negate, condition, arg),
            _ => previous
        };
    }

    public static bool CheckConditionSet(int i) => i >= 0 && i < Bartender.Configuration.ConditionSets.Count && CheckConditionSet(Bartender.Configuration.ConditionSets[i]);

    public static bool CheckConditionSet(CondSetConfig set)
    {
        if (LockedSets.Contains(set))
            return ConditionSetCache.TryGetValue(set, out var c) && c.previous;

        if (ConditionSetCache.TryGetValue(set, out var cache) && Bartender.RunTime <= cache.time + (Bartender.Configuration.NoConditionCache ? 0 : 0.1f))
            return cache.previous;

        LockedSets.Add(set);

        var first = true;
        var previous = true;
        var steps = new List<bool>();
        foreach (var cnd in set.Conditions)
        {
            var condition = GetCondition(cnd.ID);
            if (condition == null) continue;

            if (first)
            {
                previous = CheckUnaryCondition(cnd.Negate, condition, cnd.Arg);
                first = false;
            }
            else
            {
                previous = CheckBinaryCondition(previous, cnd.Operator, cnd.Negate, condition, cnd.Arg);
            }

            steps.Add(previous);
        }

        LockedSets.Remove(set);

        ConditionSetCache[set] = (previous, Bartender.RunTime);
        DebugSteps[set] = steps;
        return previous;
    }

    public static List<bool> GetDebugSteps(CondSetConfig set) => DebugSteps.TryGetValue(set, out var steps) ? steps : null;

    public static void UpdateCache()
    {
        if (Bartender.Configuration.NoConditionCache)
        {
            ConditionCache.Clear();
            return;
        }

        if (Bartender.RunTime < LastConditionCache + 0.1f) return;

        ConditionCache.Clear();
        LastConditionCache = Bartender.RunTime;
    }

    public static void SwapConditionSet(int src, int dest)
    {
        var set = Bartender.Configuration.ConditionSets[src];

        foreach (var profile in Bartender.Configuration.ProfileConfigs)
        {
            if (profile.ConditionSet == src)
                profile.ConditionSet = dest;
            else if (profile.ConditionSet == dest)
                profile.ConditionSet = src;
        }

        foreach (var condition in from s in Bartender.Configuration.ConditionSets from condition in s.Conditions where condition.ID == ConstID select condition)
        {
            if (condition.Arg == src)
                condition.Arg = dest;
            else if (condition.Arg == dest)
                condition.Arg = src;
        }

        Bartender.Configuration.ConditionSets.RemoveAt(src);
        Bartender.Configuration.ConditionSets.Insert(dest, set);
        Bartender.Configuration.Save();
    }

    public static void RemoveConditionSet(int i)
    {
        foreach (var profile in Bartender.Configuration.ProfileConfigs)
        {
            if (profile.ConditionSet > i)
                profile.ConditionSet -= 1;
            if (profile.ConditionSet == i)
                profile.ConditionSet = -1;
        }

        foreach (var s in Bartender.Configuration.ConditionSets)
        {
            for (int j = s.Conditions.Count - 1; j >= 0; j--)
            {
                var cond = s.Conditions[j];
                if (cond.ID != ConstID) continue;

                if (cond.Arg > i)
                    cond.Arg -= 1;
                else if (cond.Arg == i)
                    s.Conditions.RemoveAt(j);
            }
        }

        Bartender.Configuration.ConditionSets.RemoveAt(i);
        Bartender.Configuration.Save();
    }

    public static void ShiftCondition(CondSetConfig set, CondConfig cond, bool incr)
    {
        var i = set.Conditions.IndexOf(cond);
        if (!incr ? i <= 0 : i >= (set.Conditions.Count - 1)) return;

        var j = (incr ? i + 1 : i - 1);
        var condition = set.Conditions[i];
        set.Conditions.RemoveAt(i);
        set.Conditions.Insert(j, condition);
        Bartender.Configuration.Save();
    }
}
