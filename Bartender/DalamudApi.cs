global using Dalamud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Bartender;

public class DalamudApi
{
    #region Services

    [PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; }

    [PluginService]
    public static IChatGui ChatGui { get; private set; }

    [PluginService]
    public static IClientState ClientState { get; private set; }

    [PluginService]
    public static ICommandManager CommandManager { get; private set; }

    [PluginService]
    public static IFramework Framework { get; private set; }

    [PluginService]
    public static IGameConfig GameConfig { get; private set; }

    [PluginService]
    public static IKeyState KeyState { get; private set; }

    [PluginService]
    public static IToastGui ToastGui { get; private set; }

    [PluginService]
    public static IGameGui GameGui { get; private set; }

    [PluginService]
    public static INotificationManager NotificationManager { get; private set; }

    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; }

    [PluginService]
    public static ITextureProvider TextureProvider { get; private set; }

    [PluginService]
    public static IDataManager DataManager { get; private set; }

    [PluginService]
    public static IPluginLog PluginLog { get; private set; }

    [PluginService]
    public static Dalamud.Plugin.Services.ICondition Condition { get; private set; }

    [PluginService]
    public static IPartyList PartyList { get; private set; }

    [PluginService]
    public static ITargetManager TargetManager { get; private set; }

    [PluginService]
    public static IMarketBoard MarketBoard { get; private set; }

    [PluginService]
#pragma warning disable Dalamud001 // Le type est utilisé à des fins d’évaluation uniquement et est susceptible d’être modifié ou supprimé dans les futures mises à jour. Supprimez ce diagnostic pour continuer.
    public static IConsole Console { get; private set; }
#pragma warning restore Dalamud001 // Le type est utilisé à des fins d’évaluation uniquement et est susceptible d’être modifié ou supprimé dans les futures mises à jour. Supprimez ce diagnostic pour continuer.

    #endregion

    private static PluginCommandManager<IDalamudPlugin> PluginCommandManager;

    public DalamudApi() { }
    public DalamudApi(IDalamudPlugin plugin) => PluginCommandManager ??= new(plugin);
    public DalamudApi(IDalamudPlugin plugin, IDalamudPluginInterface pluginInterface)
    {
        if (!pluginInterface.Inject(this))
        {
            PluginLog.Error("Failed loading DalamudApi.");
            return;
        }

        PluginCommandManager ??= new(plugin);
    }

    public static DalamudApi operator +(DalamudApi container, object o)
    {
        foreach (var f in typeof(DalamudApi).GetProperties())
        {
            if (f.PropertyType != o.GetType()) continue;
            if (f.GetValue(container) != null) break;
            f.SetValue(container, o);
            return container;
        }
        throw new InvalidOperationException();
    }

    public static void Initialize(IDalamudPlugin plugin, IDalamudPluginInterface pluginInterface) => _ = new DalamudApi(plugin, pluginInterface);
    public static void Dispose() => PluginCommandManager?.Dispose();
}

#region PluginCommandManager

public class PluginCommandManager<T> : IDisposable where T : IDalamudPlugin
{
    private readonly T plugin;
    private readonly (string, CommandInfo)[] pluginCommands;

    public PluginCommandManager(T p)
    {
        plugin = p;
        pluginCommands = plugin.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
            .SelectMany(GetCommandInfoTuple)
            .ToArray();

        AddCommandHandlers();
    }

    private void AddCommandHandlers()
    {
        foreach (var (command, commandInfo) in pluginCommands)
        {
            DalamudApi.CommandManager.AddHandler(command, commandInfo);
        }
    }

    private void RemoveCommandHandlers()
    {
        foreach (var (command, _) in pluginCommands)
        {
            DalamudApi.CommandManager.RemoveHandler(command);
        }
    }

    private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(MethodInfo method)
    {
        var handlerDelegate = (IReadOnlyCommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(IReadOnlyCommandInfo.HandlerDelegate), plugin, method);

        CommandAttribute? command = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
        AliasesAttribute? aliases = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
        HelpMessageAttribute? helpMessage = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
        DoNotShowInHelpAttribute? doNotShowInHelp = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();

        CommandInfo? commandInfo = new(handlerDelegate)
        {
            HelpMessage = helpMessage?.HelpMessage ?? string.Empty,
            ShowInHelp = doNotShowInHelp == null
        };

        List<(string, CommandInfo)> commandInfoTuple =
        [
            (command?.Command, commandInfo)
        ];
        if (aliases != null)
        {
            commandInfoTuple.AddRange(aliases.Aliases.Select(alias => (alias, commandInfo)));
        }

        return commandInfoTuple;
    }

    public void Dispose()
    {
        RemoveCommandHandlers();
        GC.SuppressFinalize(this);
    }
}

#endregion

#region Attributes

[AttributeUsage(AttributeTargets.Method)]
public class AliasesAttribute : Attribute
{
    public string[] Aliases { get; }

    public AliasesAttribute(params string[] aliases)
    {
        Aliases = aliases;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Command { get; }

    public CommandAttribute(string cmd)
    {
        Command = cmd;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class DoNotShowInHelpAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class HelpMessageAttribute : Attribute
{
    public string HelpMessage { get; }

    public HelpMessageAttribute(string helpMsg)
    {
        HelpMessage = helpMsg;
    }
}

#endregion
