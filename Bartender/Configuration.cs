using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json;
using ImGuiNET;
using Dalamud.Configuration;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Bartender;

public struct HotbarSlot(uint id, HotbarSlotType type)
{
    public uint CommandId = id;
    public HotbarSlotType CommandType = type;
}

public class ProfileConfig
{
    [JsonProperty("name")][DefaultValue("")] public string Name = string.Empty;
    [JsonProperty("hotkey")][DefaultValue(0)] public int Hotkey = 0;
    [JsonProperty("hotbars")][DefaultValue(0)] public BarNums UsedBars = BarNums.None;
    [JsonProperty("slots")][DefaultValue(null)] public HotbarSlot[,] Slots = new HotbarSlot[Bartender.NUM_OF_BARS, Bartender.NUM_OF_SLOTS];

    [Flags]
    public enum BarNums
    {
        None = 0,
        One = 1 << 0,
        Two = 1 << 1,
        Three = 1 << 2,
        Four = 1 << 3,
        Five = 1 << 4,
        Six = 1 << 5,
        Seven = 1 << 6,
        Eight = 1 << 7,
        Nine = 1 << 8,
        Ten = 1 << 9
    }

    public HotbarSlot[] GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Slots.GetLength(0))
            throw new IndexOutOfRangeException("Row index is out of range.");

        HotbarSlot[] rowArray = new HotbarSlot[Slots.GetLength(1)];
        for (int j = 0; j < Slots.GetLength(1); j++)
        {
            rowArray[j] = Slots[rowIndex, j];
        }
        return rowArray;
    }
}

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string PluginVersion = ".init";
    public string GetVersion() => PluginVersion;
    public void UpdateVersion()
    {
        if (PluginVersion != ".init")
            PrevPluginVersion = PluginVersion;
        PluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }
    public bool CheckVersion() => PluginVersion == Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    [JsonIgnore] public static DirectoryInfo ConfigFolder => DalamudApi.PluginInterface.ConfigDirectory;
    [JsonIgnore] public static FileInfo ConfigFile => DalamudApi.PluginInterface.ConfigFile;
    [JsonIgnore] public string PrevPluginVersion = string.Empty;

    public List<ProfileConfig> ProfileConfigs = new();
    public bool ExportOnDelete = false;

    public void Initialize()
    {
        if (ConfigFolder.Exists)
        {
            
        }
    }

    public void Save(bool failed = false)
    {
        try
        {
            DalamudApi.PluginInterface.SavePluginConfig(this);
        }
        catch
        {
            if (!failed)
            {
                PluginLog.LogError("Failed to save. Trying again...");
                Save(true);
            }
            else
            {
                PluginLog.LogError("Failed to save.");
            }
        }
    }

    public void LoadConfig(FileInfo file)
    {
        if (!file.Exists) return;

        try
        {
            file.CopyTo(ConfigFile.FullName, true);
            Bartender.Plugin.Reload();
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to load config.");
        }
    }
}
