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

namespace Bartender.UI;

public class BartenderUI : Window, IDisposable
{
    private bool lastConfigPopupOpen = false;
    private bool configPopupOpen = false;
    private Vector2 iconButtonSize = new(26);
    private ProfileConfig? selectedProfile;
    private int? selectedProfileID;
    public bool IsConfigPopupOpen() => configPopupOpen || lastConfigPopupOpen;
    public void SetConfigPopupOpen() => configPopupOpen = true;

    public readonly List<ProfileUI> Profiles;

    public BartenderUI()
        : base($"Bartender v{Bartender.Configuration.GetVersion()}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove)
    {
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(850, 500), MaximumSize = ImGuiHelpers.MainViewport.Size };

        Profiles = new List<ProfileUI>();
        for (int i = 0; i < Bartender.Configuration.ProfileConfigs.Count; i++)
            Profiles.Add(new ProfileUI(i));
    }

    public void Reload() => Dispose();

    public override void Draw()
    {
        if (ImGui.BeginTabBar("Config Tabs"))
        {
            if (ImGui.BeginTabItem("Profiles"))
            {
                DrawProfiles();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Automation"))
            {
                ConditionSetUI.Draw(iconButtonSize);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Settings"))
            {
                DrawSettings();
                ImGui.EndTabItem();
            }
#if DEBUG
            if (ImGui.BeginTabItem("Debug"))
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
                    ImGui.SetTooltip("Create a new profile from the currently active hotbars");

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
                ImGui.SetTooltip("Import a profile from the clipboard");
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
                if (ImGui.Selectable($"Delete profile '{profile.Name}' permanently") && ImGui.GetIO().KeyShift)
                {
                    RemoveProfile(i);
                    if (selectedProfile == profile) selectedProfile = null;
                }
                ImGui.PopStyleColor();
                if (!ImGui.GetIO().KeyShift && ImGui.IsItemHovered())
                    ImGui.SetTooltip("Hold SHIFT to delete.");
                ImGui.EndPopup();
            }
        }
    }

    private void ImportProfile(string import)
    {
        ProfileConfig config = ProfileConfig.FromBase64(import);
        if (config == null)
        {
            DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
            {
                Content = $"Cannot import profile",
                Type = Dalamud.Interface.Internal.Notifications.NotificationType.Error,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(3)
            });
            return;
        }
        AddProfile(config);

        DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
        {
            Content = $"Profile imported: {config.Name}",
            Type = Dalamud.Interface.Internal.Notifications.NotificationType.Success,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(3)
        });
    }

    private void AddProfile(ProfileConfig cfg)
    {
        Bartender.Configuration.ProfileConfigs.Add(cfg);
        Profiles.Add(new ProfileUI(Profiles.Count));
        Bartender.Configuration.Save();
    }

    private void RemoveProfile(int i)
    {
        if (Bartender.Configuration.ExportOnDelete)
            ImGui.SetClipboardText("export on delete test");

        Profiles[i].Dispose();
        Profiles.RemoveAt(i);
        Bartender.Configuration.ProfileConfigs.RemoveAt(i);
        Bartender.Configuration.Save();
        RefreshProfilesIndexes();
    }

    public void ShiftProfile(int i, bool increment)
    {
        if (!increment ? i > 0 : i < (Profiles.Count - 1))
        {
            var j = (increment ? i + 1 : i - 1);
            var profile = Profiles[i];
            Profiles.RemoveAt(i);
            Profiles.Insert(j, profile);

            var profile2 = Bartender.Configuration.ProfileConfigs[i];
            Bartender.Configuration.ProfileConfigs.RemoveAt(i);
            Bartender.Configuration.ProfileConfigs.Insert(j, profile2);
            Bartender.Configuration.Save();
            selectedProfile = profile2;
            selectedProfileID = j;
            RefreshProfilesIndexes();
        }
    }

    private void RefreshProfilesIndexes()
    {
        for (int i = 0; i < Profiles.Count; i++)
            Profiles[i].ID = i;
    }

    #endregion

    private void DrawSettings()
    {
        if (ImGui.Checkbox("Export on Delete", ref Bartender.Configuration.ExportOnDelete))
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
    }

    public void Dispose()
    {
        return;
    }
}
