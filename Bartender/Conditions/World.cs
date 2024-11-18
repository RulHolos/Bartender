using Bartender.UI.Utils;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.Conditions;

public class WorldCondition : ICondition, IDrawableCondition, IArgCondition, IOnImportCondition, IConditionCategory
{
    public string ID => "wo";
    public string ConditionName => "World";
    public string CategoryName => "World";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => DalamudApi.ClientState.LocalPlayer?.CurrentWorld.RowId == (uint)arg;
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public void Draw(CondConfig cfg)
    {
        static string formatName(Lumina.Excel.Sheets.World w) => $"[{w.RowId}] {w.Name}";

        if (!ImGuiEx.ExcelSheetCombo<Lumina.Excel.Sheets.World>("##Zone",
            out var world,
            s => formatName(s.GetRow((uint)cfg.Arg)),
            ImGuiComboFlags.None, (w, s) =>
            {
                return formatName(w).Contains(s, StringComparison.CurrentCultureIgnoreCase) || s == string.Empty;
            },
            t => ImGui.Selectable(formatName(t), cfg.Arg == t.RowId))) return;

        cfg.Arg = world.RowId;
        Bartender.Configuration.Save();
    }
    public dynamic GetDefaultArg(CondConfig cfg) => DalamudApi.ClientState.LocalPlayer?.HomeWorld.RowId;
    public void OnImport(CondConfig cfg)
    {
        if (cfg.Arg == 0)
            cfg.Arg = GetDefaultArg(cfg);
    }
}
