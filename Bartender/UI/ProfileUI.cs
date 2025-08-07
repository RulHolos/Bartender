using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
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
using Dalamud.Utility;
using Lumina.Text.ReadOnly;
using System.Text;
using System.Xml.Linq;
using Lumina.Excel.Sheets;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility.Raii;
using static Bartender.ProfileConfig;

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
                ImGuiEx.SetLocalizedTooltip("tooltip.CreateProfile");
                ImGui.SameLine();
            }

            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
            {
                string import;
                try { import = ImGui.GetClipboardText(); }
                catch { import = string.Empty; }
                ImportProfile(import);
            }
            ImGuiEx.SetLocalizedTooltip("tooltip.ImportProfile");

            ImGuiComponents.HelpMarker(Localization.Get("tooltip.ProfileHelp"));
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

    public static int SwitchingProfile = -1;

    public static void DrawProfileList()
    {
        for (int i = 0; i < Bartender.Configuration.ProfileConfigs.Count; i++)
        {
            ImGui.PushID($"Bartender-ProfileLista-{i}");

            ProfileConfig profile = Bartender.Configuration.ProfileConfigs[i];

            /*float totalWidth = ImGui.GetContentRegionAvail().X;

            ImGui.Columns(2, $"BartenderProList-{profile.Name}", false);

            float buttonWidth = ImGui.CalcTextSize("↑").X + (ImGui.GetStyle().ItemSpacing.X * 2);
            buttonWidth += ImGui.CalcTextSize("↓").X + (ImGui.GetStyle().ItemSpacing.X * 2);
            buttonWidth += ImGui.GetStyle().ItemSpacing.X * 2.0f;

            float firstColumnWidth = totalWidth - buttonWidth;
            if (firstColumnWidth < 0) firstColumnWidth = 0;
            ImGui.SetColumnWidth(0, firstColumnWidth);
            ImGui.SetColumnWidth(1, buttonWidth);*/

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

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                ImGuiEx.SetupSlider(true, ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.Y, (hitInterval, increment, closing) =>
                {
                    if (hitInterval)
                        ShiftProfile(Bartender.Configuration.ProfileConfigs.IndexOf(profile), increment);
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
                });
            }
            
            if (ImGui.BeginPopupContextItem($"profile_context_menu_{i}"))
            {
                if (ImGui.MenuItem(Localization.Get("btn.SaveHotbars")))
                    SaveProfile(profile, i);
                if (ImGui.MenuItem(Localization.Get("btn.LoadProfile")))
                    Bartender.Plugin.BarLoad(profile.Name);
                if (ImGui.MenuItem(Localization.Get("btn.ClearHotbars")))
                    Bartender.Plugin.BarClear(profile.Name);

                ImGui.Separator();

                if (ImGui.MenuItem(Localization.Get("text.Export")))
                {
                    ImGui.SetClipboardText(ProfileConfig.ToBase64(profile));
                    NotificationManager.Display(Localization.Get("notify.ProfileExported", profile.Name));
                }

                ImGui.Separator();

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

            /*ImGui.NextColumn();

            if (ImGui.Button($"↑##BartenderProList-{profile.Name}"))
                ShiftProfile(i, false);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Localization.Get("tooltip.SwapToAbove"));
            ImGui.SameLine(0f, 1.5f);
            if (ImGui.Button($"↓##BartenderProList-{profile.Name}"))
                ShiftProfile(i, true);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Localization.Get("tooltip.SwapToBelow"));

            ImGui.Columns(1);*/
            
            ImGui.PopID();
        }
    }

    public static void DrawProfileEditor()
    {
        ImGui.Columns(2, $"BartenderList-{SelectedProfileId}", false);
        ImGui.PushID((int)SelectedProfileId);

        float displaySize = Bartender.Configuration.IconDisplaySize;
        try
        {
            var icon = Bartender.IconManager.GetIcon(Convert.ToUInt32(SelectedProfile.IconId));
            ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, new(displaySize), default, new Vector2(1f, 1f), 0);
        }
        catch (IconNotFoundException)
        {
            var icon = Bartender.IconManager.GetIcon(0);
            ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, new(displaySize), default, new Vector2(1f, 1f), 0);
        }
        ImGuiEx.SetItemTooltip(Localization.Get("tooltip.ProfileIcon"));

        ImGui.SameLine();

        ImGui.BeginGroup();
        {
            if (ImGui.InputInt("##ProfileIconId", ref SelectedProfile.IconId, 1))
            {
                SelectedProfile.IconId = Math.Max(0, SelectedProfile.IconId);
                Bartender.Configuration.Save();
            }
            ImGuiEx.SetItemTooltip("See \"/xldata icons\" for icon ids.");

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                SaveProfile();
            ImGuiEx.SetItemTooltip(Localization.Get("btn.SaveHotbars"));
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
                Bartender.Plugin.BarLoad("/barload", SelectedProfile.Name);
            ImGuiEx.SetItemTooltip(Localization.Get("btn.LoadProfile"));
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SquareXmark))
                Bartender.Plugin.BarClear("/barclear", SelectedProfile.Name);
            ImGuiEx.SetItemTooltip(Localization.Get("btn.ClearHotbars"));
        }
        ImGui.EndGroup();

        ImGui.NextColumn();

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##Name", ref SelectedProfile.Name, 32))
            Bartender.Configuration.Save();

        var preview = ((SelectedProfile.ConditionSet >= 0) && (SelectedProfile.ConditionSet < Bartender.Configuration.ConditionSets.Count))
            ? $"[{SelectedProfile.ConditionSet + 1}] {Bartender.Configuration.ConditionSets[SelectedProfile.ConditionSet].Name}" : Localization.Get("text.NoCondition");
        ImGui.SetNextItemWidth(-1);
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
        ImGuiEx.SetItemTooltip(Localization.Get("tooltip.ConditionSetTutorial"));

        ImGui.Columns(1);
        ImGui.Separator();

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
        /*
#if DEBUG
        ImGui.SameLine();
        ImGui.Text($"=> {((int)Bartender.Configuration.ProfileConfigs[(int)SelectedProfileId].UsedBars)}");
#endif
        */

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.BeginChild($"BartenderList-{SelectedProfileId}-Hotbars");
        for (int s = 0; s < Bartender.NUM_OF_BARS; s++)
        {
            ImGui.PushID(s);
            BarNums flag = (BarNums)(1 << s);
            if ((SelectedProfile.UsedBars & flag) == flag) {
                DrawHotbar(s);
            }
            ImGui.PopID();
        }
        ImGui.EndChild();

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
                ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, new Vector2(40), default, new Vector2(1f, 1f), 0, slotColor);
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    ImGuiEx.SetupSlider(false, ImGui.GetItemRectSize().X + ImGui.GetStyle().ItemSpacing.X, (hitInterval, increment, closing) =>
                    {
                        if (hitInterval)
                            ShiftIcon(hotbar, action, increment);
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                    });
                }
                else if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    ToggleSlotTransparent(hotbar, j);
                }
                else if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && action.CommandType != HotbarSlotType.Empty)
                {
                    SetSlotAsEmpty(hotbar, j);
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
        ImGuiEx.SetItemTooltip(Localization.Get("text.HotbarsToLoad"));
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
                if (name != null)
                {
                    var st = SeString.Parse(Encoding.UTF8.GetBytes(name)).ToString();
                    name = st;
                }
                string hint = slot->PopUpKeybindHintString;

                string fullText = $"{name} {hint}";

                var data = slot->CommandType == HotbarSlotType.Macro ? Tuple.Create(slot->CommandId, slot->CommandType) : Tuple.Create(slot->OriginalApparentActionId, slot->OriginalApparentSlotType);

                int icon = slot->GetIconIdForSlot(slot->OriginalApparentSlotType, slot->OriginalApparentActionId);

                generatedSlots[hotbars, i] = new HotbarSlot(data.Item1, data.Item2, icon, fullText);
            }
        }
        return generatedSlots;
    }

    public static unsafe void SaveProfile(ProfileConfig profile, int profileID)
    {
        if (!Bartender.AddAndExecuteCommand(new SaveProfileCommand(profileID, PopulateProfileHotbars())))
            return;
        NotificationManager.Display(Localization.Get("notify.ProfileSaved", profile.Name), NotificationType.Success, 3);
    }

    public static unsafe void SaveProfile() => SaveProfile(SelectedProfile, (int)SelectedProfileId);

    private static void ShiftIcon(int profileId, HotbarSlot slot, bool increment)
    {
        Bartender.AddAndExecuteCommand(new ShiftIconCommand(SelectedProfile, slot, increment, profileId));
    }

    private static void ToggleSlotTransparent(int profileId, int slotIdx)
    {
        Bartender.AddAndExecuteCommand(new ToggleSlotTransparencyCommand(SelectedProfile!, profileId, slotIdx));
    }

    private static void SetSlotAsEmpty(int profileId, int slotIdx)
    {
        Bartender.AddAndExecuteCommand(new SetSlotAsEmptyCommand(SelectedProfile!, profileId, slotIdx));
    }
}
