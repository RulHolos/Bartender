using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Bartender.UI.Utils;

namespace Bartender.UI;

public static class ConditionSetUI
{
    private static CondSetConfig? SelectedSet;
    private static int? SelectedSetId;
    private static readonly List<string> BinaryOperators = ["&&", " | | ", " ==", " !="];

    public static void Draw(Vector2 iconButtonSize)
    {
        ImGui.BeginGroup();
        {
            if (ImGui.BeginChild("conditionset_list", ImGuiHelpers.ScaledVector2(240, 0) - iconButtonSize with { X = 0 }, true))
            {
                DrawAutomationList();
                ImGui.EndChild();
            }

            // Create condition set
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            {
                Bartender.Configuration.ConditionSets.Add(new() { Name = "New Set" });
                Bartender.Configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Create a new condition set");

            if (Bartender.Configuration.ConditionSets.Count != 0)
            {
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport))
                {
                    string export;
                    try { export = CondSetConfig.ToBase64(SelectedSet); }
                    catch { export = string.Empty; }
                    ImGui.SetClipboardText(export);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Export a condition set to the clipboard");
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
            {
                try
                {
                    CondSetConfig? import = CondSetConfig.FromBase64(ImGui.GetClipboardText());
                    if (import != null)
                    {
                        foreach (var condition in import.Conditions)
                        {
                            if (ConditionManager.GetCondition(condition.ID) is IOnImportCondition c)
                                c.OnImport(condition);
                        }

                        Bartender.Configuration.ConditionSets.Add(import);
                        Bartender.Configuration.Save();
                    }
                }
                catch (Exception e)
                {
                    DalamudApi.PluginLog.Error($"Failed to import condition set from clipboard.\n{e.Message}");
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Import a condition set from the clipboard");
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        if (ImGui.BeginChild("conditionset_view", ImGuiHelpers.ScaledVector2(0), true))
        {
            if (SelectedSet != null)
                DrawAutomationEditor(SelectedSet);
            ImGui.EndChild();
        }
    }

    private static void DrawAutomationList()
    {
        for (int i = 0; i < Bartender.Configuration.ConditionSets.Count; i++)
        {
            CondSetConfig set = Bartender.Configuration.ConditionSets[i];

            ImGui.PushStyleColor(ImGuiCol.Text, set.Checked ? 0xFF00FF00u : 0xFF0000FFu);
            if (ImGui.Selectable($"#{i + 1}: {set.Name}", SelectedSet == set))
            {
                SelectedSet = set;
                SelectedSetId = i;
            }
            ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGui.GetIO().KeyShift ? ImGuiCol.Text : ImGuiCol.TextDisabled));
                if (ImGui.Selectable($"Delete Condition Set '{set.Name}' permanently") && ImGui.GetIO().KeyShift)
                {
                    ConditionManager.RemoveConditionSet(i);
                    if (SelectedSet == set) SelectedSet = null;
                }
                ImGui.PopStyleColor();
                if (!ImGui.GetIO().KeyShift && ImGui.IsItemHovered())
                    ImGui.SetTooltip("Hold SHIFT to delete.");
                ImGui.EndPopup();
            }
        }
    }

    private static void DrawAutomationEditor(CondSetConfig set)
    {
        var debugSteps = ConditionManager.GetDebugSteps(set);
        var comboSize = ImGui.CalcTextSize("AND").X;

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##Name", ref set.Name, 32))
            Bartender.Configuration.Save();

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, ImGui.GetStyle().ItemSpacing.Y));

        if (set.Conditions.Count == 0)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            {
                set.Conditions.Add(new());
                Bartender.Configuration.Save();
            }
            return;
        }

        for (int i = 0; i < set.Conditions.Count; i++)
        {
            ImGui.PushID(i);

            ImGui.BeginGroup();
            {
                var condCfg = set.Conditions[i];
                var selectedCond = ConditionManager.GetCondition(condCfg.ID);
                var selectedCat = ConditionManager.GetConditionCategory(selectedCond);

                ImGui.Columns(3, null, false);

                ImGuiComponents.IconButton(FontAwesomeIcon.ArrowsAltV);
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    ImGuiEx.SetupSlider(true, ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.Y, (hitInterval, increment, closing) =>
                    {
                        if (hitInterval)
                            ConditionManager.ShiftCondition(set, condCfg, increment);
                    });
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Right click to delete this condition.");
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    {
                        set.Conditions.RemoveAt(i);
                        Bartender.Configuration.Save();
                    }
                }

                ImGui.SameLine();

                if (i != 0)
                {
                    var _ = (int)condCfg.Operator;
                    ImGui.SetNextItemWidth(comboSize);
                    if (ImGui.BeginCombo("##Operator", BinaryOperators[_], ImGuiComboFlags.NoArrowButton))
                    {
                        for (int ind = 0; ind < BinaryOperators.Count; ind++)
                        {
                            var op = BinaryOperators[ind];
                            if (ImGui.Selectable(op, ind == _))
                                condCfg.Operator = (ConditionManager.BinaryOperator)ind;
                        }
                        ImGui.EndCombo();
                    }

                    var operatorTooltip = condCfg.Operator.ToString();
                    if (debugSteps != null && i < debugSteps.Count)
                    {
                        var setSuccess = debugSteps[i];
                        operatorTooltip += $"\nSet (Up to this condition): {(setSuccess ? "True" : "False")}";

                        var setStatusCol = setSuccess ? 0x2000FF00u : 0x200000FFu;
                        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), setStatusCol, ImGui.GetStyle().FrameRounding);
                    }
                    ImGuiEx.SetItemTooltip(operatorTooltip);
                }
                else
                {
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                    {
                        set.Conditions.Add(new() { ID = "Cond" });
                        Bartender.Configuration.Save();
                    }
                }

                ImGui.SameLine();

                var previousCursorPos = ImGui.GetCursorPos();
                var __ = false;
                if (ImGui.Checkbox("##NOT", ref __))
                {
                    condCfg.Negate ^= true;
                    Bartender.Configuration.Save();
                }

                var notTooltip = "IS" + (condCfg.Negate ? " NOT" : "");
                var success = ConditionManager.CheckCondition(condCfg.ID, condCfg.Arg, condCfg.Negate);
                notTooltip += $"\nCondition: {(success ? "True" : "False")}";

                var statusCol = success ? 0x2000FF00u : 0x200000FFu;
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), statusCol, ImGui.GetStyle().FrameRounding);

                ImGuiEx.SetItemTooltip(notTooltip);

                ImGui.SameLine();

                if (condCfg.Negate)
                {
                    var postCursorPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(previousCursorPos);
                    ImGui.TextUnformatted("  !=");
                    ImGui.SetCursorPos(postCursorPos);
                }

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.BeginCombo("##Category", selectedCat.CategoryName, ImGuiComboFlags.NoArrowButton))
                {
                    foreach (var (category, list) in ConditionManager.ConditionCategories)
                    {
                        if (!ImGui.Selectable(category.CategoryName, category.GetType() == selectedCat.GetType()))
                            continue;

                        var condition = list[0];
                        condCfg.ID = condition.ID;
                        condCfg.Arg = condition is IArgCondition arg ? arg.GetDefaultArg(condCfg) : 0;
                        Bartender.Configuration.Save();

                        selectedCond = ConditionManager.GetCondition(condCfg.ID);
                        selectedCat = ConditionManager.GetConditionCategory(selectedCond);
                    }

                    ImGui.EndCombo();
                }

                ImGui.NextColumn();

                var conditionList = ConditionManager.ConditionCategories.FirstOrDefault(t => t.category.GetType() == selectedCat.GetType()).conditions;
                var drawable = selectedCond as IDrawableCondition;
                var drawExtra = true;
                if (conditionList != null)
                {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (conditionList.Count == 1)
                    {
                        drawExtra = false;
                        try { drawable?.Draw(condCfg); }
                        catch (Exception e) { DalamudApi.PluginLog.Error($"Error while drawing {drawable}.\n{e}"); }
                    }
                    else if (ImGui.BeginCombo("##Condition", selectedCond.ConditionName))
                    {
                        foreach (var condition in conditionList)
                        {
                            if (ImGui.Selectable(condition.ConditionName, condition.GetType() == selectedCond.GetType()))
                            {
                                condCfg.ID = condition.ID;
                                condCfg.Arg = condition is IArgCondition arg ? arg.GetDefaultArg(condCfg) : 0;
                                Bartender.Configuration.Save();

                                selectedCond = ConditionManager.GetCondition(condCfg.ID);
                                drawable = selectedCond as IDrawableCondition;
                            }

                            var d = condition as IDrawableCondition;
                            var tooltip = d?.GetSelectableTooltip(condCfg);
                            if (!string.IsNullOrEmpty(tooltip))
                                ImGuiEx.SetItemTooltip(tooltip);
                        }
                        ImGui.EndCombo();
                    }
                    var s = drawable?.GetTooltip(condCfg);
                    if (!string.IsNullOrEmpty(s))
                        ImGuiEx.SetItemTooltip(s);
                }

                ImGui.NextColumn();

                if (drawExtra)
                {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    try { drawable?.Draw(condCfg); }
                    catch (Exception e) { DalamudApi.PluginLog.Error($"Error while drawing {drawable}.\n{e}"); }
                }
                ImGui.NextColumn();

                ImGui.PopID();
            }
        }
    }
}
