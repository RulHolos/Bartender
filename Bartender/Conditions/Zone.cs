using Bartender.UI.Utils;
using ImGuiNET;
using System;

namespace Bartender.Conditions;

public class ZoneCondition : ICondition, IDrawableCondition, IArgCondition, IConditionCategory
{
    public string ID => "z";
    public string ConditionName => "Zone";
    public string CategoryName => "Zone";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => DalamudApi.ClientState.TerritoryType == (ushort)arg;
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public void Draw(CondConfig cfg)
    {
        static string formatName(Lumina.Excel.Sheets.TerritoryType t) => $"[{t.RowId}] {t.PlaceName.Value.Name}";

        if (!ImGuiEx.ExcelSheetCombo<Lumina.Excel.Sheets.TerritoryType>("##Zone", out var territory, s => formatName(s.GetRow((uint)cfg.Arg)),
            ImGuiComboFlags.None, (t, s) => formatName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase),
            t => ImGui.Selectable(formatName(t), cfg.Arg == t.RowId))) return;

        cfg.Arg = territory.RowId;
        Bartender.Configuration.Save();
    }
    public dynamic GetDefaultArg(CondConfig cfg) => DalamudApi.ClientState.TerritoryType;
}
