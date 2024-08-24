using System;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;
using Dalamud.Interface.Utility;
using static Bartender.ProfileConfig;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Bartender.UI.Utils;
using Dalamud.Interface.Components;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using Bartender.DataCommands;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Bartender.UI;

public static class ProfileUI
{
    public static ProfileConfig? SelectedProfile;
    public static int? SelectedProfileId;

    public static void Draw(Vector2 iconButtonSize)
    {
        ImGui.BeginGroup();
        {
            if (ImGui.BeginChild("profile_list", ImGuiHelpers.ScaledVector2(240, 0) - iconButtonSize with { X = 0 }, true))
            {
                DrawProfileList();
                ImGui.EndChild();
            }

            if (DalamudApi.ClientState.IsLoggedIn != false)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                {
                    ProfileConfig newProfile = new();
                    if (Bartender.Configuration.PopulateWhenCreatingProfile)
                        newProfile.Slots = PopulateProfileHotbars();
                    AddProfile(newProfile);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Localization.Get("tooltip.CreateProfile"));
                ImGui.SameLine();
            }

            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
            {
                string import;
                try { import = ImGui.GetClipboardText(); }
                catch { import = string.Empty; }
                ImportProfile(import);
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Localization.Get("tooltip.ImportProfile"));
            }
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        if (ImGui.BeginChild("profile_view", ImGuiHelpers.ScaledVector2(0), true))
        {
            if (SelectedProfile != null)
                DrawProfileEditor();
            ImGui.EndChild();
        }
    }

    public static void DrawProfileList()
    {
        for (int i = 0; i < Bartender.Configuration.ProfileConfigs.Count; i++)
        {
            ProfileConfig profile = Bartender.Configuration.ProfileConfigs[i];

            if (profile.ConditionSet != -1)
                ImGui.PushStyleColor(ImGuiCol.Text, Bartender.Configuration.ConditionSets[profile.ConditionSet].Checked ? 0xFF00FF00u : 0xFF0000FFu);
            else
                ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFFFFFu);
            if (ImGui.Selectable($"#{i + 1}: {profile.Name}", SelectedProfile == profile))
            {
                SelectedProfile = profile;
                SelectedProfileId = i;
            }
            ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGui.GetIO().KeyShift ? ImGuiCol.Text : ImGuiCol.TextDisabled));
                if (ImGui.Selectable(Localization.Get("selectable.DeleteProfile", profile.Name)) && ImGui.GetIO().KeyShift)
                {
                    RemoveProfile(i);
                    if (SelectedProfile == profile)
                        SelectedProfile = null;
                }
                ImGui.PopStyleColor();
                if (!ImGui.GetIO().KeyShift && ImGui.IsItemHovered())
                    ImGui.SetTooltip(Localization.Get("tooltip.DeleteProfile"));
                ImGui.EndPopup();
            }
        }
    }

    public static void DrawProfileEditor()
    {
        ImGui.Columns(2, $"BartenderList-{SelectedProfileId}", false);
        ImGui.PushID((int)SelectedProfileId);

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##Name", ref SelectedProfile.Name, 32))
            Bartender.Configuration.Save();

        ImGui.NextColumn();

        if (ImGui.Button("↑"))
            ShiftProfile((int)SelectedProfileId, false);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Localization.Get("tooltip.SwapToAbove"));
        ImGui.SameLine();
        if (ImGui.Button("↓"))
            ShiftProfile((int)SelectedProfileId, true);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Localization.Get("tooltip.SwapToBelow"));
        ImGui.SameLine();
        if (ImGui.Button(Localization.Get("text.Export")))
        {
            ImGui.SetClipboardText(ProfileConfig.ToBase64(SelectedProfile));
            NotificationManager.Display(Localization.Get("notify.ProfileExported", SelectedProfile.Name));
        }
        var preview = ((SelectedProfile.ConditionSet >= 0) && (SelectedProfile.ConditionSet < Bartender.Configuration.ConditionSets.Count))
            ? $"[{SelectedProfile.ConditionSet + 1}] {Bartender.Configuration.ConditionSets[SelectedProfile.ConditionSet].Name}" : Localization.Get("text.NoCondition");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("##Condition", preview))
        {
            if (ImGui.Selectable(Localization.Get("text.None"), SelectedProfile.ConditionSet == -1))
            {
                Bartender.AddAndExecuteCommand(new ChangeConditionSetCommand(SelectedProfile.ConditionSet, -1, SelectedProfile));
            }
            for (int id = 0; id < Bartender.Configuration.ConditionSets.Count; id++)
            {
                if (ImGui.Selectable($"[{id + 1}] {Bartender.Configuration.ConditionSets[id].Name}", id == SelectedProfile.ConditionSet))
                {
                    Bartender.AddAndExecuteCommand(new ChangeConditionSetCommand(SelectedProfile.ConditionSet, id, SelectedProfile));
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Localization.Get("tooltip.ConditionSetTutorial"));

        ImGui.Separator();
        ImGui.NextColumn();
        ImGui.Columns(1);

        ImGui.Text(Localization.Get("text.HotbarsToLoad"));

        int num = 1;
        foreach (BarNums bar in Enum.GetValues(typeof(BarNums)))
        {
            if (bar == BarNums.None)
                continue; // Skip BarNums.None.
            if (num > 1 /*&& num != 6*/)
                ImGui.SameLine();
            CheckboxFlags($"#{num}", bar);
            num++;
        }

#if DEBUG
        ImGui.SameLine();
        ImGui.Text($"=> {((int)Bartender.Configuration.ProfileConfigs[(int)SelectedProfileId].UsedBars)}");
#endif
        ImGui.Spacing();

        if (ImGui.Button(Localization.Get("btn.SaveHotbars")))
            SaveProfile();
        ImGui.SameLine();
        if (ImGui.Button(Localization.Get("btn.LoadProfile")))
            Bartender.Plugin.BarLoad("/barload", SelectedProfile.Name);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Execute '/barload {SelectedProfile.Name}'");

        if (ImGui.Button(Localization.Get("btn.ClearHotbars")))
            Bartender.Plugin.BarClear("/barclear", SelectedProfile.Name);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Execute '/barclear {SelectedProfile.Name}'");

        ImGui.Spacing();
        ImGui.Separator();
        for (int s = 0; s < Bartender.NUM_OF_BARS; s++)
        {
            ImGui.PushID(s);
            BarNums flag = (BarNums)(1 << s);
            if ((SelectedProfile.UsedBars & flag) == flag) {
                DrawHotbar(s);
            }
            ImGui.PopID();
        }

        ImGui.PopID();
    }

    public static void ImportProfile(string import)
    {
        ProfileConfig? config = null;
        try { config = ProfileConfig.FromBase64(import); }
        catch { }

        if (config == null || !AddProfile(config))
        {
            NotificationManager.Display(Localization.Get("notify.CannotImport"), NotificationType.Error);
            return;
        }

        NotificationManager.Display(Localization.Get("notify.ProfileImported", config.Name));
    }

    public static bool AddProfile(ProfileConfig? cfg)
    {
        if (cfg == null)
            return false;
        return Bartender.AddAndExecuteCommand(new AddProfileCommand(cfg));
    }

    public static void RemoveProfile(int i)
    {
        if (Bartender.Configuration.ExportOnDelete)
        {
            ImGui.SetClipboardText(ProfileConfig.ToBase64(SelectedProfile));
            NotificationManager.Display(Localization.Get("notify.ProfileExported", SelectedProfile.Name));
        }

        Bartender.AddAndExecuteCommand(new RemoveProfileCommand(SelectedProfile));
    }

    public static void ShiftProfile(int i, bool increment)
    {
        if (!increment ? i > 0 : i < (Bartender.Configuration.ProfileConfigs.Count - 1))
        {
            var j = (increment ? i + 1 : i - 1);
            Bartender.AddAndExecuteCommand(new ShiftProfileCommand(i, j));
        }
    }

    private static void DrawHotbar(int hotbar)
    {
        ImGui.Text(Localization.Get("text.Hotbar", hotbar + 1));
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Localization.Get("tooltip.DragAndSlide"));
        for (int j = 0; j < Bartender.NUM_OF_SLOTS; j++)
        {
            ImGui.PushID(j);
            try
            {
                var action = SelectedProfile.Slots[hotbar, j];
                var icon = Bartender.IconManager.GetIcon(Convert.ToUInt32(action.Icon));
                Vector4 slotColor = action.Transparent ? new Vector4(0, 0, 0, 1f) : new Vector4(0);
                ImGui.ImageButton(icon.GetWrapOrEmpty().ImGuiHandle, new Vector2(35, 35), default, new Vector2(1f, 1f), 0, slotColor);
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    ImGuiEx.SetupSlider(false, ImGui.GetItemRectSize().X + ImGui.GetStyle().ItemSpacing.X, (hitInterval, increment, closing) =>
                    {
                        if (hitInterval)
                            ShiftIcon(hotbar, action, increment);
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                    });
                }
                else if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    AssignActionToSlot(hotbar, action);
                }
                else if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    ToggleSlotTransparent(hotbar, action);
                }
                else if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && action.CommandType != HotbarSlotType.Empty)
                {
                    SetSlotAsEmpty(hotbar, action);
                }
                if (ImGui.IsItemHovered() && action.CommandType != HotbarSlotType.Empty)
                {
                    ImGui.SetTooltip($"{action.Name + (action.Transparent ?
                        $"\n({Localization.Get("tooltip.TransparentSlot")})"
                        : "")}" +
                        $"\n\n[{Localization.Get("tooltip.HoldToShift")}]\n[{Localization.Get("tooltip.EmptySlot")}]\n[{Localization.Get("tooltip.ToggleTransparency")}]");
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                else if (ImGui.IsItemHovered())
                {
                    string text = action.Transparent ? Localization.Get("tooltip.TransparentSlot") : Localization.Get("tooltip.NonTransparentSlot");
                    ImGui.SetTooltip($"({text})\n\n[{Localization.Get("tooltip.ToggleTransparency")}]");
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                    
                if (j < Bartender.NUM_OF_SLOTS - 1) ImGui.SameLine();
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error($"{e}");
            }
            ImGui.PopID();
        }
        ImGui.Spacing();
        ImGui.Separator();
    }

    private static void CheckboxFlags(string label, BarNums flags)
    {
        int flagsValue = (int)Bartender.Configuration.ProfileConfigs[(int)SelectedProfileId].UsedBars;
        if (ImGui.CheckboxFlags(label, ref flagsValue, (int)flags))
        {
            Bartender.AddAndExecuteCommand(
                new ChangeUsedBarsCommand(Bartender.Configuration.ProfileConfigs[(int)SelectedProfileId].UsedBars, (BarNums)flagsValue, (int)SelectedProfileId)
            );
        }
    }

    /// <summary>
    /// Opens the action explorer with the hotbar and profile ID to assign an action to this slot.
    /// </summary>
    public static void AssignActionToSlot(int profileId, HotbarSlot slot)
    {
        Bartender.Plugin.actionExplorerUI.OpenWithSlotID(profileId, slot);
    }

    public static unsafe HotbarSlot[,] PopulateProfileHotbars()
    {
        HotbarSlot[,] generatedSlots = new HotbarSlot[Bartender.NUM_OF_BARS, Bartender.NUM_OF_SLOTS];
        for (uint hotbars = 0; hotbars < Bartender.NUM_OF_BARS; hotbars++)
        {
            for (uint i = 0; i < Bartender.NUM_OF_SLOTS; i++)
            {
                RaptureHotbarModule.HotbarSlot* slot = Bartender.RaptureHotbar->GetSlotById(hotbars, i);
                if (slot->CommandType == HotbarSlotType.Empty)
                    slot->IconId = 0;
                
                string name = Utf8StringMarshaller.ConvertToManaged(slot->GetDisplayNameForSlot(slot->OriginalApparentSlotType, slot->OriginalApparentActionId))!;
                string hint = slot->PopUpKeybindHintString;

                string fullText = $"{name} {hint}";
                int icon = slot->GetIconIdForSlot(slot->OriginalApparentSlotType, slot->OriginalApparentActionId);
                
                generatedSlots[hotbars, i] = new HotbarSlot(slot->OriginalApparentActionId, slot->OriginalApparentSlotType, icon, fullText);
            }
        }
        return generatedSlots;
    }

    public static unsafe void SaveProfile()
    {
        if (!Bartender.AddAndExecuteCommand(new SaveProfileCommand((int)SelectedProfileId, PopulateProfileHotbars())))
            return;
        NotificationManager.Display(Localization.Get("notify.ProfileSaved", SelectedProfile.Name), NotificationType.Success, 3);
    }

    private static void ShiftIcon(int profileId, HotbarSlot slot, bool increment)
    {
        Bartender.AddAndExecuteCommand(new ShiftIconCommand(SelectedProfile, slot, increment, profileId));
    }

    private static void ToggleSlotTransparent(int profileId, HotbarSlot slot)
    {
        Bartender.AddAndExecuteCommand(new ToggleSlotTransparencyCommand(SelectedProfile, slot, profileId));
    }

    private static void SetSlotAsEmpty(int profileId, HotbarSlot slot)
    {
        Bartender.AddAndExecuteCommand(new SetSlotAsEmptyCommand(SelectedProfile, slot, profileId));
    }
}
