using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender;

public static class IpcProvider
{
    public const uint MajorVersion = 1;
    public const uint MinorVersion = 0;

    public const string Namespace = "Bartender";

    private static ICallGateProvider<(uint, uint)>? ApiVersion;

    internal static void Init()
    {
        ApiVersion = DalamudApi.PluginInterface.GetIpcProvider<(uint, uint)>($"{Namespace}.{nameof(ApiVersion)}");
        ApiVersion.RegisterFunc(() => (MajorVersion, MinorVersion));
    }

    internal static void DeInit()
    {
        ApiVersion?.UnregisterFunc();
    }
}
