using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Dalamud.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Linq;

namespace Bartender.UI;

public static class GearsetContextMenu
{
    private static DalamudContextMenu? ContextMenu;

    public static void Initialize()
    {
        ContextMenu = new(DalamudApi.PluginInterface);
        ContextMenu.OnOpenGameObjectContextMenu += AddMenu;
    }

    private unsafe static void AddMenu(GameObjectContextMenuOpenArgs args)
    {
        if (args.ParentAddonName == "GearSetList")
        {
            IntPtr GearSetAgent = DalamudApi.GameGui.FindAgentInterface(args.ParentAddonName);
            var GearSetId = *(uint*)(GearSetAgent);

            DalamudApi.PluginLog.Error((*(uint*)(GearSetAgent + 0x1bc)).ToString());

            for (int i = 0; i < 10000; i++)
            {
                string val = (*(uint*)(GearSetAgent + (0x0 + i))).ToString();
                if (val == "27")
                    DalamudApi.PluginLog.Error(i + ": " + val);
            }

            args.AddCustomItem(new GameObjectContextMenuItem(
                new SeString(new UIForegroundPayload(706), new TextPayload($"{SeIconChar.BoxedLetterB.ToIconString()} "), UIForegroundPayload.UIForegroundOff,
                new TextPayload("Bind Bartender Profile")), _ => BindToProfile()));

            args.AddCustomItem(new GameObjectContextMenuItem(
                new SeString(new UIForegroundPayload(706), new TextPayload($"{SeIconChar.BoxedLetterB.ToIconString()} "), UIForegroundPayload.UIForegroundOff,
                new TextPayload("Unbind Bartender Profile")), _ => UnbindFromProfile()));
        }
    }

    private static void BindToProfile()
    {

    }

    private static void UnbindFromProfile()
    {

    }

    public static void Dispose()
    {
        ContextMenu.OnOpenGameObjectContextMenu -= AddMenu;
        ContextMenu.Dispose();
    }
}
