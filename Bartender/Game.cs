using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace Bartender;

public unsafe class Game
{
    public static UIModule* uiModule;
    public static int CurrentHUDLayout => uiModule->GetAddonConfig()->ModuleData->CurrentHudLayout;

    public static void Initialize()
    {
        uiModule = Framework.Instance()->GetUIModule();

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

    public static void Dispose()
    {

    }
}
