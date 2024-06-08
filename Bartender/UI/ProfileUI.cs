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
using FFXIVClientStructs.FFXIV.Client.Game;

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

    public void DrawConfig(BartenderUI mainUI, int i)
    {
        ImGui.Columns(2, $"BartenderList-{i}", false);
        ImGui.PushID(i);

        ImGui.Text($"#{i + 1}");
        ImGui.SameLine();

        float textX = ImGui.GetCursorPosX();

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##Name", ref Config.Name, 32))
            Bartender.Configuration.Save();

        ImGui.NextColumn();

        if (ImGui.Button("↑"))
            mainUI.ShiftProfile(i, false);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Swap profile position with the one above.");
        ImGui.SameLine();
        if (ImGui.Button("↓"))
            mainUI.ShiftProfile(i, true);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Swap profile position with the one below.");
        ImGui.SameLine();
        if (ImGui.Button("Export"))
        {
            ImGui.SetClipboardText(ProfileConfig.ToBase64(Config));
            DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
            {
                Content = $"Profile exported and copied to clipboard: {Config.Name}",
                Type = Dalamud.Interface.Internal.Notifications.NotificationType.Success,
                Minimized = false,
                InitialDuration = TimeSpan.FromSeconds(3)
            });
        }
        var preview = ((Config.ConditionSet >= 0) && (Config.ConditionSet < Bartender.Configuration.ConditionSets.Count))
            ? $"[{Config.ConditionSet + 1}] {Bartender.Configuration.ConditionSets[Config.ConditionSet].Name}" : "No conditions...";
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("##Condition", preview))
        {
            if (ImGui.Selectable("None", Config.ConditionSet == -1))
            {
                Config.ConditionSet = -1;
                Bartender.Configuration.Save();
            }
            for (int id = 0; id < Bartender.Configuration.ConditionSets.Count; id++)
            {
                if (ImGui.Selectable($"[{id + 1}] {Bartender.Configuration.ConditionSets[id].Name}", id == Config.ConditionSet))
                {
                    Config.ConditionSet = id;
                    Bartender.Configuration.Save();
                }
            }
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Condition Set to check. Will load this profile when the condition set is set to true.\n" +
                "Leave this empty if you do not wish to load this profile automatically.\n" +
                "Warning: Profile ordering matters: Profiles with an higher id will be higher in the priority list.");

        ImGui.Separator();
        ImGui.NextColumn();
        ImGui.Columns(1);

        ImGui.Text("Hotbars to load when using '/barload'");

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
        ImGui.Text($"=> {((int)Bartender.Configuration.ProfileConfigs[ID].UsedBars)}");
#endif
        ImGui.Spacing();

        if (ImGui.Button("Save current hotbars"))
            SaveProfile();
        ImGui.SameLine();
        if (ImGui.Button("Load this profile"))
            Bartender.Plugin.BarLoad("/barload", Config.Name);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Executes '/barload {Config.Name}'");
        

        if (ImGui.Button("Revert to game")) { }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Doesn't do anything yet");
        ImGui.SameLine();
        if (ImGui.Button("Clear profile's hotbars"))
            Bartender.Plugin.BarClear("/barclear", Config.Name);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Executes '/barclear {Config.Name}'");

        ImGui.Spacing();
        ImGui.Separator();
        int currentBar = -1;
        int currentSlot = -1;
        for (int s = 0; s < Bartender.NUM_OF_BARS; s++)
        {
            BarNums flag = (BarNums)(1 << s);
            if ((Config.UsedBars & flag) != flag)
                continue;
            DrawHotbar(s);
        }

        ImGui.PopID();
    }

    private unsafe void DrawHotbar(int hotbar)
    {
        ImGui.Text($"Hotbar #{hotbar + 1}");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("You can drag and slide icons to re-arrange them inside this hotbar");
        for (int j = 0; j < Bartender.NUM_OF_SLOTS; j++)
        {
            try
            {
                ImGui.PushID(j);

                var action = Config.Slots[hotbar, j];
                var icon = Bartender.IconManager.GetIcon(Convert.ToUInt32(action.Icon));
                ImGui.ImageButton(icon.ImGuiHandle, new Vector2(35, 35), default, new Vector2(1f, 1f), 0, new Vector4(0));
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    ImGuiEx.SetupSlider(false, ImGui.GetItemRectSize().X + ImGui.GetStyle().ItemSpacing.X, (hitInterval, increment, closing) =>
                    {
                        if (hitInterval)
                            ShiftIcon(hotbar, action, increment);
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                    });
                }
                if (ImGui.IsItemHovered() && action.CommandType != HotbarSlotType.Empty)
                {
                    ImGui.SetTooltip($"{action.Name}");
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                    
                if (j < Bartender.NUM_OF_SLOTS - 1) ImGui.SameLine();

                ImGui.PopID();
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error($"{e}");
            }
        }
        ImGui.Spacing();
        ImGui.Separator();
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
                if (slot->CommandType == HotbarSlotType.Empty)
                    slot->Icon = 0;
                string fullText = slot->PopUpHelp.ToString();
                Bartender.Configuration.ProfileConfigs[ID].Slots[hotbars, i] =
                    new HotbarSlot(slot->CommandId, slot->CommandType, slot->Icon, fullText);
            }
        }
        Bartender.Configuration.Save();
        DalamudApi.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification()
        {
            Content = $"Profile saved: {Config.Name}",
            Type = Dalamud.Interface.Internal.Notifications.NotificationType.Success,
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(3)
        });
    }

    private void ShiftIcon(int profileId, HotbarSlot slot, bool increment)
    {
        HotbarSlot[] hotbar = Config.GetRow(profileId);
        int i = hotbar.ToList().IndexOf(slot);
        if (!increment ? i > 0 : i < (hotbar.Length - 1))
        {
            int j = (increment ? i + 1 : i - 1);
            HotbarSlot oldSlot = hotbar[i];
            HotbarSlot newSlot = hotbar[j];
            hotbar[i] = newSlot;
            hotbar[j] = oldSlot;

            Config.SetRow(hotbar, profileId);
            Bartender.Configuration.Save();
        }
    }

    public void Dispose()
    {
        return;
    }
}
