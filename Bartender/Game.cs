using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace Bartender;

public unsafe class Game
{
    public static UIModule* uiModule;

    public static nint addonConfig;
    [Signature("E8 ?? ?? ?? ?? 4D 8B 4D 50")]
    private static delegate* unmanaged<nint, byte> getHUDLayout;
    public static byte CurrentHUDLayout => getHUDLayout(addonConfig);

    public static void Initialize()
    {
        uiModule = Framework.Instance()->GetUiModule();

        addonConfig = ((delegate* unmanaged<UIModule*, nint>)uiModule->vfunc[19])(uiModule);

        DalamudApi.GameInteropProvider.InitializeFromAttributes(new Game());
    }

    private readonly Dictionary<uint, Action?> actionCache = [];

    public Action? GetAction(uint actionId)
    {
        var adjustedActionId = ActionManager.Instance()->GetAdjustedActionId(actionId);

        if (actionCache.TryGetValue(adjustedActionId, out var action)) return action;

        action = DalamudApi.DataManager.GetExcelSheet<Action>()!.GetRow(adjustedActionId);
        actionCache.Add(adjustedActionId, action);
        return action;
    }
}
