using System;
using Dalamud.Game.ClientState.Conditions;
using ImGuiNET;

namespace Bartender.Conditions;

public class ConditionFlagCondition : ICondition, IDrawableCondition, IConditionCategory
{
    private static readonly Array ConditionFlags = Enum.GetValues(typeof(ConditionFlag));

    public string ID => "cf";
    public string ConditionName => "Condition Flag";
    public string CategoryName => "Condition Flag";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => DalamudApi.Condition[(ConditionFlag)arg];
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public void Draw(CondConfig cfg)
    {
        if (ImGui.BeginCombo("##Flag", ((ConditionFlag)cfg.Arg).ToString()))
        {
            foreach (ConditionFlag flag in ConditionFlags)
            {
                if (!ImGui.Selectable(flag.ToString(), (int)flag == cfg.Arg))
                    continue;

                cfg.Arg = (int)flag;
                Bartender.Configuration.Save();
            }
            ImGui.EndCombo();
        }
    }
}
