using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bartender;

public interface ICondition
{
    public string ID { get; }
    public string ConditionName { get; }
    public bool Check(dynamic arg);
}

public interface IDrawableCondition
{
    public string GetTooltip(ConditionConfig cndCfg);
    public string GetSelectableTooltip(ConditionConfig cndCfg);
    public void Draw(ConditionConfig cndCfg);
}

public interface IArgCondition
{
    public dynamic GetDefaultArg(ConditionConfig cndCfg);
}

public static class ConditionManager
{
    private static readonly Dictionary<string, ICondition> conditions = new();
    private static readonly Dictionary<(ICondition, dynamic), bool> conditionCache = new();

    public static void Initialize()
    {
        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(ICondition)) && !t.IsInterface))
        {
            var condition = (ICondition)Activator.CreateInstance(t);
            if (condition == null) continue;

            conditions.Add(condition.ID, condition);
        }
    }

    public static ICondition GetCondition(string id) => conditions.TryGetValue(id, out var condition) ? condition : null;

    private static bool CheckCondition(ICondition condition, dynamic arg)
    {
        if (conditionCache.TryGetValue((condition, arg), out bool cache))
            return cache;

        try
        {
            cache = condition.Check(arg);
        }
        catch
        {
            cache = false;
        }

        conditionCache[(condition, arg)] = cache;
        return cache;
    }
}


#region Conditions

public class HUDLayoutCondition : ICondition, IDrawableCondition, IArgCondition
{
    public string ID => "hl";
    public string ConditionName => "Current HUD Layout";
    public bool Check(dynamic arg) => (byte)arg == Game.CurrentHUDLayout;
    public string GetTooltip(ConditionConfig cndCfg) => null;
    public string GetSelectableTooltip(ConditionConfig cndCfg) => null;
    public void Draw(ConditionConfig cndCfg)
    {
        var _ = (int)cndCfg.Arg + 1;
        if (ImGui.SliderInt("##HUDLayout", ref _, 1, 4))
            cndCfg.Arg = _ - 1;
    }
    public dynamic GetDefaultArg(ConditionConfig cndCfg) => Game.CurrentHUDLayout;
}

#endregion
