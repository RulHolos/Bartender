using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Utility;
using Dalamud.Logging;

namespace Bartender.Windows;

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
    public bool IsConfigPopupOpen() => configPopupOpen || lastConfigPopupOpen;
    public void SetConfigPopupOpen() => configPopupOpen = true;

    private bool _displayOutsideMain = true;

    private static Vector2 mousePos = ImGui.GetMousePos();

    public BartenderUI()
    {

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
        ImGui.SetNextWindowSizeConstraints(new Vector2(610, 650) * ImGuiHelpers.GlobalScale, ImGuiHelpers.MainViewport.Size);
        ImGui.Begin("Bartender Configuration", ref configOpen);

        if (ImGui.BeginTabBar("Config Tabs"))
        {
            if (ImGui.BeginTabItem("Profiles"))
            {
                // Be able to add new profiles from the UI. Be able to check individual bars to use in the profile.
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Settings"))
            {
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Debug"))
            {
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    public void Dispose()
    {

    }
}
