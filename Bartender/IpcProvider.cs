using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender;

public static class IpcProvider
{
    public const uint Breaking = 3;
    public const uint Features = 0;
    public const uint Build = 0;

    public const string Namespace = "Bartender";

    public static ICallGateProvider<(uint, uint, uint)>? ApiVersion;
    public static ICallGateProvider<object> Initialized;
    public static ICallGateProvider<object> Disposed;
    public static ICallGateProvider<string[]>? GetProfiles;
    public static ICallGateProvider<string[]>? GetConditionSets;
    public static ICallGateProvider<int, bool>? CheckConditionSet;

    internal static void Init()
    {
        ApiVersion = DalamudApi.PluginInterface.GetIpcProvider<(uint, uint, uint)>($"{Namespace}.{nameof(ApiVersion)}");
        ApiVersion.RegisterFunc(() => (Breaking, Features, Build));

        Initialized = DalamudApi.PluginInterface.GetIpcProvider<object>($"{Namespace}.Initialized");
        Disposed = DalamudApi.PluginInterface.GetIpcProvider<object>($"{Namespace}.Disposed");

        GetProfiles = DalamudApi.PluginInterface.GetIpcProvider<string[]>($"{Namespace}.GetProfiles");
        GetProfiles.RegisterFunc(() => Bartender.Configuration.ProfileConfigs.Select(s => s.Name).ToArray());

        GetConditionSets = DalamudApi.PluginInterface.GetIpcProvider<string[]>($"{Namespace}.GetConditionSets");
        GetConditionSets.RegisterFunc(() => Bartender.Configuration.ConditionSets.Select(s => s.Name).ToArray());

        CheckConditionSet = DalamudApi.PluginInterface.GetIpcProvider<int, bool>($"{Namespace}.CheckConditionSet");
        CheckConditionSet.RegisterFunc(i => i >= 0 && i < Bartender.Configuration.ConditionSets.Count && ConditionManager.CheckConditionSet(i));
    }

    internal static void DeInit()
    {
        ApiVersion?.UnregisterFunc();
        GetProfiles.UnregisterFunc();
        GetConditionSets?.UnregisterFunc();
        CheckConditionSet?.UnregisterFunc();
    }
}
