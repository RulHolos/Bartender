using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Action = Lumina.Excel.GeneratedSheets2.Action;

namespace Bartender.UI;

public class ActionExplorerUI : Window, IDisposable
{
    private int? ProfileHotbarIndex;

    public ActionExplorerUI()
        : base($"Bartender Action Explorer", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(500, 600), MaximumSize = ImGuiHelpers.MainViewport.Size };
    }

    public override void OnClose()
    {
        ProfileHotbarIndex = 0;
        base.OnClose();
    }

    public void Reload() => Dispose();

    public void OpenWithSlotID(int profileHotbarID, HotbarSlot slot)
    {
        ProfileHotbarIndex = profileHotbarID;
        IsOpen = !IsOpen;

        foreach (var type in DalamudApi.DataManager.GetExcelSheet<Action>())
        {
            DalamudApi.PluginLog.Debug(type.ActionCategory.Value.Name);
        }
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("Action Types"))
        {
            
            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    public void Dispose()
    {
        return;
    }
}
