using Bartender.UI.Utils;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.Conditions;

public class GearsetCondition : ICondition, IDrawableCondition, IConditionCategory
{
    public string ID => "gs";
    public string ConditionName => "Gearset";
    public string CategoryName => "Gearset";
    public int DisplayPriority => 0;
    public unsafe bool Check(dynamic arg) => RaptureGearsetModule.Instance()->CurrentGearsetIndex == (int)arg;
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public unsafe void Draw(CondConfig cfg)
    {
        var _ = (int)cfg.Arg + 1;
        if (ImGui.SliderInt("##GearsetIndex", ref _, 1, 100))
        {
            cfg.Arg = _ - 1;
            Bartender.Configuration.Save();
        }
    }
}
