using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiNET;
using Bartender.UI;
using Newtonsoft.Json.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Text.RegularExpressions;
using static Bartender.ProfileConfig;

namespace Bartender;

public unsafe class Bartender : IDalamudPlugin
{
    public const int NUM_OF_BARS = 10;
    public const int NUM_OF_SLOTS = 12;

    public static Bartender Plugin { get; private set; }
    public static Configuration Configuration { get; private set; }

    public BartenderUI UI;
    private bool isPluginReady = false;

    public static RaptureHotbarModule* RaptureHotbar { get; private set; } = Framework.Instance()->UIModule->GetRaptureHotbarModule();

    public Bartender(DalamudPluginInterface pluginInterface)
    {
        Plugin = this;
        DalamudApi.Initialize(this, pluginInterface);

        Configuration = (Configuration)DalamudApi.PluginInterface.GetPluginConfig() ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();

        DalamudApi.Framework.Update += Update;

        UI = new BartenderUI();
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfig;
        DalamudApi.PluginInterface.UiBuilder.Draw += Draw;

        ReadyPlugin();
    }

    public void ReadyPlugin()
    {
        try
        {
            isPluginReady = true;
        }
        catch (Exception e)
        {
            PluginLog.Error($"Failed to load Bartender.\n{e}");
        }
    }

    public void Reload()
    {
        Configuration = (Configuration)DalamudApi.PluginInterface.GetPluginConfig() ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();
        Configuration.Save();
        UI.Reload();
        DalamudApi.ChatGui.Print("plugin reload.");
    }

    public void ToggleConfig() => UI.ToggleConfig();

    #region Commands
    [Command("/bartender")]
    [HelpMessage("Open the configuration menu.")]
    public void ToggleConfig(string command, string arguments) => ToggleConfig();

    [Command("/barload")]
    [HelpMessage("Load a bar profile (meant for macros). Usage: /barload <profile name>")]
    public void BarLoad(string command, string arguments)
    {
        if (arguments.IsNullOrEmpty())
            DalamudApi.ChatGui.PrintError("Wrong arguments. Usage: /barload <profile name>");

        ProfileConfig? profile = Configuration.ProfileConfigs.Find(profile => profile.Name == arguments);
        if (profile == null)
            DalamudApi.ChatGui.PrintError($"The profile '{arguments}' does not exist.");
        for (int i = 0; i < 10; i++)
        {
            BarNums flag = (BarNums)(1 << i);
            if ((profile.UsedBars & flag) != flag)
                continue;

            HotbarSlot[] slots = profile.GetRow(i);
            for (uint slot = 0; slot < NUM_OF_SLOTS; slot++)
            {
                HotBarSlot* gameSlot = RaptureHotbar->GetSlotById(Convert.ToUInt32(i), slot);
                gameSlot->Set(slots[slot].CommandType, slots[slot].CommandId);
                RaptureHotbar->WriteSavedSlot(RaptureHotbar->ActiveHotbarClassJobId, Convert.ToUInt32(i), slot, gameSlot, false, DalamudApi.ClientState.IsPvP);
            }
        }
    }
    #endregion

    //public static bool IsLoggedIn() => ConditionManager.CheckCondition("1");

    public static float RunTime => (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds;
    public static long FrameCount => (long)DalamudApi.PluginInterface.UiBuilder.FrameCount;

    private void Update(IFramework framework)
    {
        if (!isPluginReady) return;

        //ConditionManager.UpdateCache();
    }

    private void Draw()
    {
        if (!isPluginReady) return;

        UI.Draw();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        Configuration.Save();

        DalamudApi.Framework.Update -= Update;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfig;

        DalamudApi.Dispose();

        UI.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
