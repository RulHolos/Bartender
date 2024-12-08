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
using Dalamud.Interface.Utility.Raii;
using System.Linq;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Game.ClientState.Conditions;

namespace Bartender.UI;

public class ProfileHotbar : Window, IDisposable
{
    private bool DrawConfig = false;
    private bool IsHidden = false;

    internal static bool InBattle => DalamudApi.Condition[ConditionFlag.InCombat];
    private static bool GposeActive => DalamudApi.Condition[ConditionFlag.WatchingCutscene];
    private static bool CutsceneActive =>
        DalamudApi.Condition[ConditionFlag.OccupiedInCutSceneEvent]
        || DalamudApi.Condition[ConditionFlag.WatchingCutscene78]
        || DalamudApi.Condition[ConditionFlag.BetweenAreas] || DalamudApi.Condition[ConditionFlag.BetweenAreas51];

    public ProfileHotbar()
        : base($"Bartender Profiles Hotbar",
            ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoTitleBar)
    {
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(50, 50), MaximumSize = ImGuiHelpers.MainViewport.Size };
        ResetFlags();
    }

    public override void Draw()
    {
        if (!DalamudApi.ClientState.IsLoggedIn || CutsceneActive || GposeActive)
            return;

        if (ImGuiComponents.IconButton(Bartender.Configuration.ProfileHotbarLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen))
        {
            Bartender.Configuration.ProfileHotbarLocked = !Bartender.Configuration.ProfileHotbarLocked;
            ResetFlags();
        }
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            DrawConfig = !DrawConfig;
        ImGui.Separator();

        DrawConfiguration();
        DrawHotbars();
    }

    private void DrawConfiguration()
    {
        if (!DrawConfig)
            return;

        if (ImGui.InputInt("Maximum slots displayed", ref Bartender.Configuration.ProfileHotbarMaxCount))
            Bartender.Configuration.Save();
        if (ImGui.InputInt("Maximum columns", ref Bartender.Configuration.ProfileHotbarMaxColumns))
            Bartender.Configuration.Save();
        if (ImGui.Checkbox("Window background", ref Bartender.Configuration.ProfileHotbarBackground))
        {
            ResetFlags();
            Bartender.Configuration.Save();
        }
    }

    private void UpdateSlotListSize()
    {
        int desiredSlotCount = Bartender.Configuration.ProfileHotbarMaxCount;

        if (Bartender.Configuration.ProfileHotbarSlotsIndexes.Count < desiredSlotCount)
        {
            Bartender.Configuration.ProfileHotbarSlotsIndexes.AddRange(Enumerable.Repeat(-1, desiredSlotCount - Bartender.Configuration.ProfileHotbarSlotsIndexes.Count));
        }
        else if (Bartender.Configuration.ProfileHotbarSlotsIndexes.Count > desiredSlotCount)
        {
            Bartender.Configuration.ProfileHotbarSlotsIndexes.RemoveRange(desiredSlotCount, Bartender.Configuration.ProfileHotbarSlotsIndexes.Count - desiredSlotCount);
        }
    }

    private void DrawHotbars()
    {
        if (DrawConfig)
            return;

        UpdateSlotListSize();

        float displaySize = Bartender.Configuration.IconDisplaySizeHotbar;
        for (int i = 0; i < Bartender.Configuration.ProfileHotbarMaxCount; i++)
        {
            int maxColumns = Bartender.Configuration.ProfileHotbarMaxColumns;
            int row = i / maxColumns; // Current row
            int column = i % maxColumns; // Current column

            ImGui.PushID($"Bartender-ProfileHotbar-{i}");

            if (Bartender.Configuration.ProfileHotbarSlotsIndexes[i] == -1)
            {
                var icon = Bartender.IconManager.GetIcon(0);
                ImGui.ImageButton(icon.GetWrapOrEmpty().ImGuiHandle, new(displaySize));
            }
            else
            {
                try
                {
                    ProfileConfig profile = Bartender.Configuration.ProfileConfigs[Bartender.Configuration.ProfileHotbarSlotsIndexes[i]];
                    var icon = Bartender.IconManager.GetIcon(Convert.ToUInt32(profile.IconId));
                    if (ImGui.ImageButton(icon.GetWrapOrEmpty().ImGuiHandle, new(displaySize)))
                        Bartender.Plugin.BarLoad(profile.Name);
                    ImGuiEx.SetItemTooltip(profile.Name);
                }
                catch (IconNotFoundException)
                {
                    ImGui.ImageButton(Bartender.IconManager.GetIcon(0).GetWrapOrEmpty().ImGuiHandle,
                        new(displaySize));
                }
            }
            
            if (ImGui.BeginPopupContextItem($"Bartender-ProfileHotbarCtx-{i}"))
            {
                string preview = Bartender.Configuration.ProfileHotbarSlotsIndexes[i] == -1
                    ? "No Profile..."
                    : Bartender.Configuration.ProfileConfigs[Bartender.Configuration.ProfileHotbarSlotsIndexes[i]].Name;
                if (ImGui.BeginCombo("##ProfileList", preview))
                {
                    if (ImGui.Selectable(Localization.Get("text.None"), i == -1))
                    {
                        Bartender.Configuration.ProfileHotbarSlotsIndexes[i] = -1;
                        Bartender.Configuration.Save();
                    }
                    for (int id = 0; id < Bartender.Configuration.ProfileConfigs.Count; id++)
                    {
                        if (ImGui.Selectable($"[{id + 1}] {Bartender.Configuration.ProfileConfigs[id].Name}", id == Bartender.Configuration.ProfileHotbarSlotsIndexes[i]))
                        {
                            Bartender.Configuration.ProfileHotbarSlotsIndexes[i] = id;
                            Bartender.Configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.EndPopup();
            }

            if (column < maxColumns - 1 && i < Bartender.Configuration.ProfileHotbarMaxCount - 1)
                ImGui.SameLine();

            ImGui.PopID();
        }
    }

    private void ResetFlags()
    {
        if (Bartender.Configuration.ProfileHotbarLocked)
            Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        else
            Flags &= ~(ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);

        if (Bartender.Configuration.ProfileHotbarBackground)
            Flags &= ~ImGuiWindowFlags.NoBackground;
        else
            Flags |= ImGuiWindowFlags.NoBackground;
    }

    public void Dispose()
    {
        return;
    }
}
