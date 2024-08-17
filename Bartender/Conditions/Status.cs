using Bartender.UI.Utils;
using ImGuiNET;
using System.Linq;
using System;
using Dalamud.Game.ClientState.Statuses;

namespace Bartender.Conditions;

public class StatusCondition : ICondition, IDrawableCondition, IArgCondition, IConditionCategory
{
    public string ID => "bf";
    public string ConditionName => "Has Status Effect";
    public string CategoryName => "Has Status Effect";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => DalamudApi.ClientState.LocalPlayer is { } player && player.StatusList.Any(x => x.StatusId == (uint)arg);
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public void Draw(CondConfig cfg)
    {
        static string formatName(Lumina.Excel.GeneratedSheets.Status t) => $"[{t.RowId}] {t.Name}";

        if (!ImGuiEx.ExcelSheetCombo<Lumina.Excel.GeneratedSheets.Status>("##StatusEffect", out var status, s => formatName(s.GetRow((uint)cfg.Arg)),
            ImGuiComboFlags.None, (t, s) => formatName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase),
            t => ImGui.Selectable(formatName(t), cfg.Arg == t.RowId))) return;

        cfg.Arg = status.RowId;
        Bartender.Configuration.Save();
    }
    public dynamic GetDefaultArg(CondConfig cfg)
    {
        return (uint)0;
    }
}
