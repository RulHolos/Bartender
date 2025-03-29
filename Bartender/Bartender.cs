using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Bartender.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Bartender.UI.Utils;
using Dalamud.Interface.Windowing;
using static Bartender.ProfileConfig;
using System.Collections.Generic;
using Bartender.DataCommands;
using System.Text.RegularExpressions;
using Dalamud.Interface.ImGuiNotification;
using System.Linq;

namespace Bartender;

public unsafe class Bartender : IDalamudPlugin
{
    public const int NUM_OF_BARS = 10;
    public const int NUM_OF_SLOTS = 12;

    public static Bartender Plugin { get; private set; }
    public static Configuration Configuration { get; private set; }
    public static IconManager IconManager { get; private set; }
    public static Game Game { get; private set; }

    public BartenderUI UI;
    public ProfileHotbar ProfileHotbar;
    private bool isPluginReady = false;

    public readonly WindowSystem WindowSystem = new("Bartender");

    public static RaptureHotbarModule* RaptureHotbar { get; private set; } = Framework.Instance()->UIModule->GetRaptureHotbarModule();

    public Stack<DataCommand> CommandStack = [];
    public Stack<DataCommand> UndoCommandStack = [];

    public Bartender(IDalamudPluginInterface pluginInterface)
    {
        Plugin = this;
        DalamudApi.Initialize(this, pluginInterface);

        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();

        IconManager = new();
        Game = new();

        DalamudApi.Framework.Update += Update;
        

        UI = new BartenderUI() {
#if DEBUG
            IsOpen = true,
#endif
        };
        ProfileHotbar = new ProfileHotbar()
        {
            IsOpen = Configuration.UseProfileHotbar,
        };

        WindowSystem.AddWindow(UI);
        WindowSystem.AddWindow(ProfileHotbar);

        DalamudApi.PluginInterface.UiBuilder.Draw += Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenMainUi += ToggleConfig;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfig;

        ReadyPlugin();
    }

    public void ReadyPlugin()
    {
        try
        {
            IpcProvider.Init();

            Localization.Setup();
            Game.Initialize();
            ConditionManager.Initialize();

            isPluginReady = true;
            IpcProvider.Initialized.SendMessage();
            
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error($"Failed to load Bartender.\n{e}");
            isPluginReady = false;
        }
    }

    public void Reload()
    {
        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();
        Configuration.Save();
    }

    #region Commands

    public static void Undo()
    {
        Plugin.CommandStack.Peek().Undo();
        Plugin.UndoCommandStack.Push(Plugin.CommandStack.Pop());
    }

    public static void Redo()
    {
        Plugin.UndoCommandStack.Peek().Execute();
        Plugin.CommandStack.Push(Plugin.UndoCommandStack.Pop());
    }

    public static bool AddAndExecuteCommand(DataCommand command)
    {
        if (command == null)
            return true;

        Plugin.CommandStack.Push(command);
        Plugin.CommandStack.Peek().Execute();
        Plugin.UndoCommandStack = [];
        return true;
    }

    #endregion

    public void ToggleConfig() => UI.Toggle();
    public void ToggleProfileHotbar() => ProfileHotbar.Toggle();

    #region Commands

    [Command("/bartender")]
    [HelpMessage("Open the configuration menu.")]
    public void ToggleConfig(string command, string arguments) => ToggleConfig();

    [Command("/barhotbar")]
    [HelpMessage("Open the Profiles Hotbar window.")]
    public void ToggleProfileHotbar(string command, string arguments) => ToggleProfileHotbar();

    [Command("/barsave")]
    [HelpMessage("Saves the current hotbars into an existing profile. Usage: /barsave <profile name>")]
    public void BarSave(string command, string arguments)
    {
        if (arguments.IsNullOrEmpty())
            DalamudApi.ChatGui.PrintError(Localization.Get("error.Usage") + "/barsave <profile name>");

        TransformArguments(ref arguments);

        ProfileConfig? prof = Configuration.ProfileConfigs.Find(x => x.Name == arguments);
        if (prof == null)
            return;

        if (!AddAndExecuteCommand(new SaveProfileCommand(Configuration.ProfileConfigs.IndexOf(prof), ProfileUI.PopulateProfileHotbars())))
            return;
        NotificationManager.Display(Localization.Get("notify.ProfileSaved", prof.Name), NotificationType.Success, 3);
    }

    [Command("/barload")]
    [HelpMessage("Load a bar profile (meant for macros). Usage: /barload <profile name>")]
    public void BarLoad(string command, string arguments)
    {
        if (arguments.IsNullOrEmpty())
            DalamudApi.ChatGui.PrintError(Localization.Get("error.Usage") + "/barload <profile name>");

        TransformArguments(ref arguments);

        ProfileConfig? profile = Configuration!.ProfileConfigs.Find(profile => profile.Name == arguments);
        if (profile == null)
        {
            DalamudApi.ChatGui.PrintError(Localization.Get("error.ProfileNull", arguments));
            return;
        }
        BarControl(profile!, false);
    }
    public void BarLoad(string arguments) => BarLoad("/barload", arguments);

    [Command("/barclear")]
    [HelpMessage("Clears a bar profile (meant for macros). Usage: /barclear <profile name>")]
    public void BarClear(string command, string arguments)
    {
        if (arguments.IsNullOrEmpty())
            DalamudApi.ChatGui.PrintError(Localization.Get("error.Usage") + "/barclear <profile name>");

        TransformArguments(ref arguments);

        ProfileConfig? profile = Configuration!.ProfileConfigs.Find(profile => profile.Name == arguments);
        if (profile == null)
        {
            DalamudApi.ChatGui.PrintError(Localization.Get("error.ProfileNull", arguments));
            return;
        }
        BarControl(profile!, true);
    }
    public void BarClear(string arguments) => BarClear("/barclear", arguments);

    private void BarControl(ProfileConfig profile, bool clear)
    {
        for (int i = 0; i < 10; i++)
        {
            BarNums flag = (BarNums)(1 << i);
            if ((profile.UsedBars & flag) != flag)
                continue;

            HotbarSlot[] slots = profile.GetRow(i);
            for (uint slot = 0; slot < NUM_OF_SLOTS; slot++)
            {
                RaptureHotbarModule.HotbarSlot* gameSlot = RaptureHotbar->GetSlotById(Convert.ToUInt32(i), slot);
                if (clear)
                    gameSlot->Set(RaptureHotbarModule.HotbarSlotType.Empty, 0);
                else if (slots[slot].Transparent)
                    continue;
                else
                    gameSlot->Set(slots[slot].CommandType, slots[slot].CommandId);
                RaptureHotbar->WriteSavedSlot(RaptureHotbar->ActiveHotbarClassJobId, Convert.ToUInt32(i), slot, gameSlot, false, DalamudApi.ClientState.IsPvP);
            }
        }
    }

    private void TransformArguments(ref string args)
    {
        if (DalamudApi.ClientState.LocalPlayer == null)
            return;

        Regex reg = new(@"\{(\w+)\}", RegexOptions.IgnoreCase);

        args = reg.Replace(args, match =>
        {
            string vari = match.Groups[1].Value;
            string replacement = string.Empty;
            switch (vari.ToLower())
            {
                case "job": // The name of the job is all lowercase in french.
                    replacement = DalamudApi.ClientState.LocalPlayer.ClassJob.Value.Name.ToString();
                    break;
                case "jobshort":
                    replacement = DalamudApi.ClientState.LocalPlayer.ClassJob.Value.Abbreviation.ToString();
                    break;
                case "lvl":
                case "level":
                    replacement = DalamudApi.ClientState.LocalPlayer.Level.ToString();
                    break;
            }

            return replacement;
        });
    }

    #endregion

    public static float RunTime => (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds;
    public static long FrameCount => (long)DalamudApi.PluginInterface.UiBuilder.FrameCount;

    private void Update(IFramework framework)
    {
        if (!isPluginReady)
            return;

        Configuration.DoAutomaticBackup();

        ConditionManager.UpdateCache();
        for (int i = 0; i < Configuration.ConditionSets.Count; i++)
            ConditionManager.CheckConditionSet(i);
        foreach (var profile in Configuration.ProfileConfigs)
        {
            if (profile.ConditionSet == -1)
                continue;
            else if (!profile.IsAlreadyAutomaticallySet && Configuration.ConditionSets[profile.ConditionSet].Checked)
            {
                BarLoad(profile.Name);
                profile.IsAlreadyAutomaticallySet = true;
            }
            else if (profile.IsAlreadyAutomaticallySet && !Configuration.ConditionSets[profile.ConditionSet].Checked)
                profile.IsAlreadyAutomaticallySet = false;
        }
    }

    private void Draw()
    {
        if (!isPluginReady) return;

        WindowSystem.Draw();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        IpcProvider.Disposed.SendMessage();
        IpcProvider.DeInit();

        Configuration.Save();

        DalamudApi.Framework.Update -= Update;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfig;
        DalamudApi.PluginInterface.UiBuilder.Draw -= Draw;
        DalamudApi.Dispose();

        IconManager.Dispose();
        UI.Dispose();
        Game.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
