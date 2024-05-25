using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
