using ImGuiNET;

namespace Bartender.Conditions;

public class ConditionSetCondition : ICondition, IDrawableCondition, IConditionCategory
{
    public string ID => ConditionManager.ConstID;
    public string ConditionName => "Condition Set";
    public string CategoryName => "Condition Set";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => ConditionManager.CheckConditionSet((int)arg);
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public void Draw(CondConfig cfg)
    {
        var i = (int)cfg.Arg;
        if (!ImGui.BeginCombo("##Sets", (i >= 0 && i < Bartender.Configuration.ConditionSets.Count) ? $"[{i + 1}] {Bartender.Configuration.ConditionSets[i].Name}" : string.Empty))
            return;

        for (int ind = 0; ind < Bartender.Configuration.ConditionSets.Count; ind++)
        {
            var s = Bartender.Configuration.ConditionSets[ind];
            if (!ImGui.Selectable($"[{ind + 1}] {s.Name}", ind == i))
                continue;

            cfg.Arg = ind;
            Bartender.Configuration.Save();
        }
        ImGui.EndCombo();
    }
}
