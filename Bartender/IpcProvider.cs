using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender;

public static class IpcProvider
{
    public const uint Breaking = 1;
    public const uint Features = 1;
    public const uint Build = 5;

    public const string Namespace = "Bartender";

    public static ICallGateProvider<(uint, uint, uint)>? ApiVersion;
    public static ICallGateProvider<object> Initialized;
    public static ICallGateProvider<object> Disposed;
    public static ICallGateProvider<string[]>? GetProfiles;
    public static ICallGateProvider<string[]>? GetConditionSets;
    public static ICallGateProvider<int, bool>? CheckConditionSet;
    public static ICallGateProvider<Dictionary<string, string>>? GetCurrentLangDict;
    public static ICallGateProvider<string, object?>? LoadProfile;

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

        GetCurrentLangDict = DalamudApi.PluginInterface.GetIpcProvider<Dictionary<string, string>>($"{Namespace}.GetCurrentLangDict");
        GetCurrentLangDict.RegisterFunc(() => Localization.LangDict);

        LoadProfile = DalamudApi.PluginInterface.GetIpcProvider<string, object?>($"{Namespace}.LoadProfile");
        LoadProfile.RegisterAction((name) => Bartender.Plugin.BarLoad("/barload", name));
    }

    internal static void DeInit()
    {
        ApiVersion?.UnregisterFunc();
        GetProfiles.UnregisterFunc();
        GetConditionSets?.UnregisterFunc();
        CheckConditionSet?.UnregisterFunc();
        GetCurrentLangDict?.UnregisterFunc();
    }
}
