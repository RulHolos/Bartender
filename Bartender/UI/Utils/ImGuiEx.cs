using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Component.Excel;
using ImGuiNET;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.UI.Utils;

public static class ImGuiEx
{
    public static void SetItemTooltip(string s, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (ImGui.IsItemHovered(flags))
            ImGui.SetTooltip(s);
    }

    private static bool SliderEnabled = false;
    private static bool SliderVertical = false;
    private static float SliderInterval = 0;
    private static int LastHitInterval = 0;
    private static Action<bool, bool, bool> SliderAction;
    public static void SetupSlider(bool vertical, float interval, Action<bool, bool, bool> action)
    {
        SliderEnabled = true;
        SliderVertical = vertical;
        SliderInterval = interval;
        LastHitInterval = 0;
        SliderAction = action;
    }

    public static void DoSlider()
    {
        if (!SliderEnabled) return;

        var popupOpen = !ImGui.IsPopupOpen("_SLIDER") && ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopup);
        if (!popupOpen)
        {
            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(new Vector2(-100));
            ImGui.OpenPopup("_SLIDER", ImGuiPopupFlags.NoOpenOverItems);
            if (!ImGui.BeginPopup("_SLIDER")) return;
        }

        var drag = SliderVertical ? ImGui.GetMouseDragDelta().Y : ImGui.GetMouseDragDelta().X;
        var dragInterval = (int)(drag / SliderInterval);
        var hit = false;
        var increment = false;
        if (dragInterval > LastHitInterval)
        {
            hit = true;
            increment = true;
        }
        else if (dragInterval < LastHitInterval)
        {
            hit = true;
        }

        var closing = !ImGui.IsMouseDown(ImGuiMouseButton.Left);

        if (LastHitInterval != dragInterval)
        {
            while (LastHitInterval != dragInterval)
            {
                LastHitInterval += increment ? 1 : -1;
                SliderAction(hit, increment, closing && LastHitInterval == dragInterval);
            }
        }
        else
            SliderAction(false, false, closing);

        if (closing)
            SliderEnabled = false;

        if (!popupOpen)
            ImGui.EndPopup();
    }

    private static string search = string.Empty;
    public static bool ExcelSheetCombo<T>(
        string id,
        out T selected,
        Func<ExcelSheet<T>, string> getPreview,
        ImGuiComboFlags flags,
        Func<T, string, bool> searchPredicate,
        Func<T, bool> selectableDrawing) where T : struct, IExcelRow<T>
    {
        var sheet = DalamudApi.DataManager.GetExcelSheet<T>();
        return ExcelSheetCombo(id, out selected, getPreview(sheet), flags, sheet, searchPredicate, selectableDrawing);
    }

    public static bool ExcelSheetCombo<T>(string id,
        out T selected,
        string preview,
        ImGuiComboFlags flags,
        ExcelSheet<T> sheet,
        Func<T, string, bool> searchPredicate,
        Func<T, bool> drawRow) where T : struct, IExcelRow<T>
    {
        HashSet<T> filtered = [];
        selected = default;
        if (!ImGui.BeginCombo(id, preview, flags))
            return false;

        if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
        {
            search = string.Empty;
            filtered = null;
            ImGui.SetKeyboardFocusHere(0);
        }

        if (ImGui.InputText("##ExcelSheetComboSearch", ref search, 128))
            filtered = null;

        DalamudApi.PluginLog.Debug(sheet.Count.ToString());
        filtered = sheet.Where(s => searchPredicate(s, search)).ToHashSet();
        DalamudApi.PluginLog.Debug(filtered.Count.ToString());

        var i = 0;
        foreach (var row in filtered.Cast<T>())
        {
            ImGui.PushID(i++);
            if (drawRow(row))
                selected = row;
            ImGui.PopID();

            if (selected.Equals(default(T)))
                continue;
            ImGui.EndCombo();

            return true;
        }

        ImGui.EndCombo();
        return false;
    }
}
