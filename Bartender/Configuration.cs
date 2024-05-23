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

namespace Bartender;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    [JsonIgnore] public static DirectoryInfo ConfigFolder => DalamudApi.PluginInterface.ConfigDirectory;
    [JsonIgnore] private static DirectoryInfo iconFolder;
    [JsonIgnore] private static FileInfo iconCache;
    [JsonIgnore] public static FileInfo ConfigFile => DalamudApi.PluginInterface.ConfigFile;

    public void Initialize()
    {
        if (ConfigFolder.Exists)
        {
            iconFolder = new DirectoryInfo(Path.Combine(ConfigFolder.FullName, "icons"));
            iconCache = new FileInfo(ConfigFolder.FullName + "\\iconCache.json");
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

    public string GetPluginIconPath()
    {
        try
        {
            if (!iconFolder.Exists)
                iconFolder.Create();
            return iconFolder.FullName;
        }
        catch (Exception e)
        {
            PluginLog.LogError(e, "Failed to create icon folder.");
            return string.Empty;
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

    public void SaveIconCache(HashSet<int> cache)
    {
        if (!iconFolder.Exists)
            iconFolder.Create();
        try
        {
            File.WriteAllText(iconCache.FullName, JsonConvert.SerializeObject(cache, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }));
        }
        catch { }
    }

    public HashSet<int> LoadIconCache()
    {
        if (!iconFolder.Exists)
            iconFolder.Create();
        try
        {
            return JsonConvert.DeserializeObject<HashSet<int>>(File.ReadAllText(iconCache.FullName));
        }
        catch
        {
            return null;
        }
    }

    public void DeleteIconCache() { try { iconCache.Delete(); } catch { } }
}
