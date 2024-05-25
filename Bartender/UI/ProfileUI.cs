using System;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;
using Dalamud.Interface.Utility;
using static Bartender.ProfileConfig;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Bartender.UI;

public class ProfileUI : IDisposable
{
    public ProfileConfig Config { get; private set; }

    private int id;
    public int ID
    {
        get => id;
        set
        {
            id = value;
            Config = Bartender.Configuration.ProfileConfigs[value];
        }
    }

    public bool Showing = false;

    public ProfileUI(int n)
    {
        ID = n;
    }

    public void DrawConfig(ProfileConfig profile)
    {
        ImGui.Columns(1);

        ImGui.Text("Hotbars to load when using '/barload'");

        int num = 1;
        foreach (BarNums bar in Enum.GetValues(typeof(BarNums)))
        {
            if (bar == BarNums.None) continue; // Skip BarNums.None.
            if (num > 1 /*&& num != 6*/) ImGui.SameLine();
            CheckboxFlags($"#{num}", bar);
            num++;
        }
        
#if DEBUG
        ImGui.SameLine();
        ImGui.Text($"=> {((int)Bartender.Configuration.ProfileConfigs[ID].UsedBars)}");
#endif
        ImGui.Spacing();

        if (ImGui.Button("Save current hotbars"))
            SaveProfile();
        ImGui.SameLine();
        if (ImGui.Button("Load this profile"))
            Bartender.Plugin.BarLoad("/barload", profile.Name);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Executes '/barload {profile.Name}'");
        

        if (ImGui.Button("Revert to game")) { }
        ImGui.SameLine();
        if (ImGui.Button("Clear profile's hotbars"))
            Bartender.Plugin.BarClear("/barclear", profile.Name);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Executes '/barclear {profile.Name}'");

        /*
        ImGui.Spacing();
        ImGui.Separator();

        if (ImGui.Checkbox("Load on HUD Layout change", ref profile.OnHUDChange))
            Bartender.Configuration.Save();
        ImGui.SameLine();
        if (ImGui.SliderInt("##HUDLayout", ref profile.HUDLayout, 1, 4))
            Bartender.Configuration.Save();
        */
    }

    private void CheckboxFlags(string label, BarNums flags)
    {
        int flagsValue = (int)Bartender.Configuration.ProfileConfigs[ID].UsedBars;
        if (ImGui.CheckboxFlags(label, ref flagsValue, (int)flags))
        {
            Bartender.Configuration.ProfileConfigs[ID].UsedBars = (BarNums)flagsValue;
            Bartender.Configuration.Save();
        }
    }

    private unsafe void SaveProfile()
    {
        for (uint hotbars = 0; hotbars < Bartender.NUM_OF_BARS; hotbars++)
        {
            for (uint i = 0; i < Bartender.NUM_OF_SLOTS; i++)
            {
                HotBarSlot* slot = Bartender.RaptureHotbar->GetSlotById(hotbars, i);
                Bartender.Configuration.ProfileConfigs[ID].Slots[hotbars, i] = new HotbarSlot(slot->CommandId, slot->CommandType);
                //ImGui.Text($"CommandId={slot->CommandId} | CommandType={slot->CommandType}");
            }
        }
        Bartender.Configuration.Save();
        DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
        {
            Content = $"Profil saved: {Config.Name}",
            Type = Dalamud.Interface.Internal.Notifications.NotificationType.Success,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(3)
        });
    }

    public void Dispose()
    {
        return;
    }
}
