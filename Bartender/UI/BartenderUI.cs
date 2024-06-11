using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using System.Collections.Generic;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Game;
using Bartender.UI.Utils;
using Bartender.DataCommands;

namespace Bartender.UI;

public class BartenderUI : Window, IDisposable
{
    private bool lastConfigPopupOpen = false;
    private bool configPopupOpen = false;
    private Vector2 iconButtonSize = new(26);
    public ProfileConfig? selectedProfile;
    public int? selectedProfileID;
    public bool IsConfigPopupOpen() => configPopupOpen || lastConfigPopupOpen;
    public void SetConfigPopupOpen() => configPopupOpen = true;

    public readonly List<ProfileUI> Profiles;

    public BartenderUI()
        : base($"Bartender v{Bartender.Configuration.GetVersion()}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(850, 500), MaximumSize = ImGuiHelpers.MainViewport.Size };

        Profiles = new List<ProfileUI>();
        for (int i = 0; i < Bartender.Configuration.ProfileConfigs.Count; i++)
            Profiles.Add(new ProfileUI(i));
    }

    public void Reload() => Dispose();

    public override void Draw()
    {
        if (Bartender.Plugin.CommandStack.Count <= 0)
            ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Undo))
            Bartender.Undo();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Undo: {Bartender.Plugin.CommandStack.Peek()}");
        if (Bartender.Plugin.CommandStack.Count <= 0)
            ImGui.EndDisabled();
        ImGui.SameLine();
        if (Bartender.Plugin.UndoCommandStack.Count <= 0)
            ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Redo))
            Bartender.Redo();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Redo: {Bartender.Plugin.UndoCommandStack.Peek()}");
        if (Bartender.Plugin.UndoCommandStack.Count <= 0)
            ImGui.EndDisabled();

        if (ImGui.BeginTabBar("Config Tabs"))
        {
            if (ImGui.BeginTabItem(Localization.Get("tab.Profiles")))
            {
                DrawProfiles();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(Localization.Get("tab.Automation")))
            {
                ConditionSetUI.Draw(iconButtonSize);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(Localization.Get("tab.Settings")))
            {
                DrawSettings();
                ImGui.EndTabItem();
            }
#if DEBUG
            if (ImGui.BeginTabItem(Localization.Get("tab.Debug")))
            {
                DrawDebug();
                ImGui.EndTabItem();
            }
#endif

            ImGui.EndTabBar();
        }

        ImGuiEx.DoSlider();

        ImGui.End();
    }

    #region Profiles

    private void DrawProfiles()
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
                    AddProfile(new ProfileConfig());
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
            if (selectedProfile != null)
            {
                int i = (int)selectedProfileID!;
                Profiles[i].DrawConfig(this, i);
            }
            ImGui.EndChild();
        }
    }

    private void DrawProfileList()
    {
        for (int i = 0; i < Profiles.Count; i++)
        {
            ProfileConfig profile = Bartender.Configuration.ProfileConfigs[i];

            if (ImGui.Selectable($"#{i + 1}: {profile.Name}", selectedProfile == profile))
            {
                selectedProfile = profile;
                selectedProfileID = i;
            }
            if (ImGui.BeginPopupContextItem())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGui.GetIO().KeyShift ? ImGuiCol.Text : ImGuiCol.TextDisabled));
                if (ImGui.Selectable(Localization.Get("selectable.DeleteProfile", profile.Name)) && ImGui.GetIO().KeyShift)
                {
                    RemoveProfile(i);
                    if (selectedProfile == profile) selectedProfile = null;
                }
                ImGui.PopStyleColor();
                if (!ImGui.GetIO().KeyShift && ImGui.IsItemHovered())
                    ImGui.SetTooltip(Localization.Get("tooltip.DeleteProfile"));
                ImGui.EndPopup();
            }
        }
    }

    private void ImportProfile(string import)
    {
        ProfileConfig? config = null;
        try { config = ProfileConfig.FromBase64(import); }
        catch { }
        
        if (config == null)
        {
            DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
            {
                Content = Localization.Get("notify.CannotImport"),
                Type = Dalamud.Interface.Internal.Notifications.NotificationType.Error,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(3)
            });
            return;
        }
        if (!AddProfile(config))
            return;

        DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
        {
            Content = Localization.Get("notify.ProfileImported", config.Name),
            Type = Dalamud.Interface.Internal.Notifications.NotificationType.Success,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(3)
        });
    }

    private bool AddProfile(ProfileConfig cfg)
    {
        return Bartender.AddAndExecuteCommand(new AddProfileCommand(cfg));
    }

    public void RemoveProfile(int i)
    {
        if (Bartender.Configuration.ExportOnDelete)
        {
            ImGui.SetClipboardText(ProfileConfig.ToBase64(Profiles[i].Config));
            DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
            {
                Content = Localization.Get("notify.ProfileExported", Profiles[i].Config.Name),
                Type = Dalamud.Interface.Internal.Notifications.NotificationType.Success,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(3)
            });
        }

        Bartender.AddAndExecuteCommand(new RemoveProfileCommand(Profiles[i].Config));
    }

    public void ShiftProfile(int i, bool increment)
    {
        if (!increment ? i > 0 : i < (Profiles.Count - 1))
        {
            var j = (increment ? i + 1 : i - 1);
            var profile = Profiles[i];
            Bartender.AddAndExecuteCommand(new ShiftProfileCommand(i, j, profile));
        }
    }

    public void RefreshProfilesIndexes()
    {
        for (int i = 0; i < Profiles.Count; i++)
            Profiles[i].ID = i;
    }

    #endregion

    private void DrawSettings()
    {
        if (ImGui.Checkbox(Localization.Get("conf.ExportOnDelete"), ref Bartender.Configuration.ExportOnDelete))
            Bartender.Configuration.Save();

        /*
        ImGui.SameLine();
        if (ImGui.Checkbox("Use Penumbra", ref Bartender.Configuration.UsePenumbra))
            Bartender.Configuration.Save();
        */
    }

    private void DrawDebug()
    {
        ImGui.TextUnformatted("Addon Config (HUD Layout #)");
        ImGui.NextColumn();
        ImGui.Text($"0x{Game.addonConfig:X}");
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{Game.CurrentHUDLayout}");
        ImGui.NextColumn();

        if (ImGui.TreeNode("Command History"))
        {
            foreach (DataCommand command in Bartender.Plugin.CommandStack)
            {
                ImGui.Text($"{command.GetType().Name}");
            }
            ImGui.TreePop();
        }
    }

    public void Dispose()
    {
        return;
    }
}
