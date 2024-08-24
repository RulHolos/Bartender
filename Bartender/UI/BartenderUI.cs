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
    private readonly Vector2 iconButtonSize = new(26);

    public BartenderUI()
        : base($"Bartender v{Bartender.Configuration.GetVersion()}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(850, 500), MaximumSize = ImGuiHelpers.MainViewport.Size };
    }

    public void Reload() => Dispose();

    public override void Draw()
    {
        DrawDisabledButtons(Bartender.Plugin.CommandStack, FontAwesomeIcon.Undo);
        ImGui.SameLine();
        DrawDisabledButtons(Bartender.Plugin.UndoCommandStack, FontAwesomeIcon.Redo);

        if (ImGui.BeginTabBar("Config Tabs"))
        {
            if (ImGui.BeginTabItem(Localization.Get("tab.Profiles")))
            {
                ProfileUI.Draw(iconButtonSize);
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

    private void DrawDisabledButtons(Stack<DataCommand> stack, FontAwesomeIcon icon)
    {
        if (stack.Count <= 0)
            ImGui.BeginDisabled();
        if (ImGuiComponents.IconButton(icon))
            if (icon == FontAwesomeIcon.Redo)
                Bartender.Redo();
            else
                Bartender.Undo();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"{Enum.GetName(icon)}: {((stack.Count != 0) ? stack.Peek() : "---")}");
        if (stack.Count <= 0)
            ImGui.EndDisabled();
    }

    private void DrawSettings()
    {
        if (ImGui.Checkbox(Localization.Get("conf.ExportOnDelete"), ref Bartender.Configuration.ExportOnDelete))
            Bartender.Configuration.Save();
        if (ImGui.Checkbox(Localization.Get("conf.PopulateWhenCreatingProfile"), ref Bartender.Configuration.PopulateWhenCreatingProfile))
            Bartender.Configuration.Save();
    }

    private void DrawDebug()
    {
        ImGui.TextUnformatted("Addon Config (HUD Layout #)");
        ImGui.TextUnformatted($"{Game.CurrentHUDLayout}");
        ImGui.NextColumn();

        if (ImGui.TreeNode("Command History"))
        {
            foreach (DataCommand command in Bartender.Plugin.CommandStack)
                ImGui.Text(command.ToString());
            ImGui.TreePop();
        }
    }

    public void Dispose()
    {
        return;
    }
}
