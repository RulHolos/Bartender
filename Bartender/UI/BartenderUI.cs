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

namespace Bartender.UI;

public class BartenderUI : IDisposable
{
    public bool IsVisible => true;

#if DEBUG
    public bool configOpen = true;
#else
    public bool configOpen = false;
#endif

    public void ToggleConfig() => configOpen = !configOpen;

    private bool lastConfigPopupOpen = false;
    private bool configPopupOpen = false;
    private Vector2 iconButtonSize = new(26);
    private ProfileConfig? selectedProfile;
    private int? selectedProfileID;
    public bool IsConfigPopupOpen() => configPopupOpen || lastConfigPopupOpen;
    public void SetConfigPopupOpen() => configPopupOpen = true;

    private bool _displayOutsideMain = true;

    private static Vector2 mousePos = ImGui.GetMousePos();

    public readonly List<ProfileUI> Profiles;

    public BartenderUI()
    {
        Profiles = new List<ProfileUI>();
        for (int i = 0; i < Bartender.Configuration.ProfileConfigs.Count; i++)
        {
            Profiles.Add(new ProfileUI(i));
        }
    }

    public void Reload()
    {
        Dispose();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        mousePos = ImGui.GetMousePos();

        lastConfigPopupOpen = configPopupOpen;
        configPopupOpen = false;

        if (configOpen)
            DrawPluginConfig();
    }

    private void DrawPluginConfig()
    {
        ImGui.SetNextWindowSizeConstraints(new Vector2(850, 500) * ImGuiHelpers.GlobalScale, ImGuiHelpers.MainViewport.Size);
        ImGui.Begin($"Bartender v{Bartender.Configuration.GetVersion()}", ref configOpen);

        if (ImGui.BeginTabBar("Config Tabs"))
        {
            if (ImGui.BeginTabItem("Profiles"))
            {
                // Be able to add new profiles from the UI. Be able to check individual bars to use in the profile.
                // Add a button to allow to save the currnt UI into an already-existing profile.
                DrawProfiles();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Settings"))
            {
                DrawSettings();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebug();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void DrawProfiles()
    {
        Vector2 textSize = new(-1, 0);
        float textX = 0.0f;

        ImGui.BeginGroup();
        {
            if (ImGui.BeginChild("profile_list", ImGuiHelpers.ScaledVector2(240, 0) - iconButtonSize with { X = 0 }, true))
            {
                DrawProfileList(textSize, textX);
                ImGui.EndChild();
            }

            var profileListPos = ImGui.GetItemRectSize().X;

            if (DalamudApi.ClientState.IsLoggedIn != false)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                {
                    AddProfile(new ProfileConfig());
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Create a new profile from the currently active hotbars.");

                ImGui.SameLine();
            }

            if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
            {
                // Do import.
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Import a profile from the clipboard.");
            }
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        if (ImGui.BeginChild("profile_view", ImGuiHelpers.ScaledVector2(0), true))
        {
            if (selectedProfile != null)
            {
                DrawProfileSettings();
            }
            ImGui.EndChild();
        }
    }

    private void DrawProfileList(Vector2 textSize, float textX)
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

    private void DrawProfileSettings()
    {
        ProfileConfig profile = selectedProfile!;
        int i = (int)selectedProfileID!;

        ImGui.Columns(2, $"BartenderList-{i}", false);
        ImGui.PushID(i);

        ImGui.Text($"#{i + 1}");
        ImGui.SameLine();

        float textX = ImGui.GetCursorPosX();

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##Name", ref profile.Name, 32))
            Bartender.Configuration.Save();

        ImGui.NextColumn();

        if (ImGui.Button("↑"))
            ShiftProfile(i, false);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Swap profile position with the one above.");
        ImGui.SameLine();
        if (ImGui.Button("↓"))
            ShiftProfile(i, true);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Swap profile position with the one below.");
        ImGui.SameLine();
        if (ImGui.Button("Export"))
        {
            // Do export
        }

        ImGui.Separator();
        ImGui.NextColumn();

        Profiles[i].DrawConfig(profile);

        ImGui.PopID();
    }

    private void DrawSettings()
    {
        if (ImGui.Checkbox("Export on Delete", ref Bartender.Configuration.ExportOnDelete))
            Bartender.Configuration.Save();
    }

    private void DrawDebug()
    {
        ImGui.TextUnformatted("Addon Config (HUD Layout #)");
        ImGui.NextColumn();
        ImGui.Text($"{Game.addonConfig:X}");
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{Game.CurrentHUDLayout}");
        ImGui.NextColumn();
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
        RefreshBarIndexes();
    }

    private void ShiftProfile(int i, bool increment)
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
            RefreshBarIndexes();
        }
    }

    private void RefreshBarIndexes()
    {
        for (int i = 0; i < Profiles.Count; i++)
            Profiles[i].ID = i;
    }

    public void Dispose()
    {

    }
}
