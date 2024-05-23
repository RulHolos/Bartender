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
using Bartender.Windows;
using Newtonsoft.Json.Linq;

namespace Bartender;

public class Bartender : IDalamudPlugin
{
    public static Bartender Plugin { get; private set; }
    public static Configuration Configuration { get; private set; }

    public BartenderUI UI;
    private bool isPluginReady = false;

    public Bartender(DalamudPluginInterface pluginInterface)
    {
        Plugin = this;
        DalamudApi.Initialize(this, pluginInterface);

        Configuration = (Configuration)DalamudApi.PluginInterface.GetPluginConfig() ?? new();
        Configuration.Initialize();

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
        Configuration.Save();
        UI.Reload();
    }

    public void ToggleConfig() => UI.ToggleConfig();

    #region Commands
    [Command("/bartender")]
    [HelpMessage("Open the configuration menu.")]
    public void ToggleConfig(string command, string arguments) => ToggleConfig();

    [Command("/bar")]
    [HelpMessage("Save or load a hotbar profile. Usage: /bar [save|load] <profile name>")]
    public void BarControl(string command, string arguments)
    {
        return;
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
