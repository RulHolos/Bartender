using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json;
using Dalamud.Configuration;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Text;
using System.IO.Compression;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace Bartender;

public struct HotbarSlot(uint id, HotbarSlotType type, int icon, string name, bool transparent = false)
{
    public string Name = name;
    public uint CommandId = id;
    public HotbarSlotType CommandType = type;
    public int Icon = icon;
    public bool Transparent = transparent;
}

public class ProfileConfig
{
    [JsonProperty("name")][DefaultValue("")] public string Name = string.Empty;
    [JsonProperty("iconId")][DefaultValue(0)] public int IconId = 0;
    [JsonProperty("hotkey")][DefaultValue(0)] public int Hotkey = 0;
    [JsonProperty("hotbars")][DefaultValue(0)] public BarNums UsedBars = BarNums.None;
    [JsonProperty("slots")][DefaultValue(null)] public HotbarSlot[,] Slots = new HotbarSlot[Bartender.NUM_OF_BARS, Bartender.NUM_OF_SLOTS];
    [JsonProperty("condset")][DefaultValue(-1)] public int ConditionSet = -1;
    [JsonIgnore] public bool IsAlreadyAutomaticallySet = false;

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
            rowArray[j] = Slots[rowIndex, j];
        return rowArray;
    }

    public void SetRow(HotbarSlot[] row, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Slots.GetLength(0))
            throw new IndexOutOfRangeException("Row index is out of range.");

        for (int j = 0; j < Slots.GetLength(1); j++)
            Slots[rowIndex, j] = row[j];
    }

    public static string ToBase64(ProfileConfig conf)
    {
        string seri = JsonConvert.SerializeObject(conf);
        var bytes = Encoding.UTF8.GetBytes(seri);
        using var ms = new MemoryStream();
        using (var gs = new GZipStream(ms, CompressionMode.Compress))
            gs.Write(bytes, 0, bytes.Length);
        return Convert.ToBase64String(ms.ToArray());
    }

    public static ProfileConfig? FromBase64(string s)
    {
        var data = Convert.FromBase64String(s);
        using var ms = new MemoryStream(data);
        using var gs = new GZipStream(ms, CompressionMode.Decompress);
        using var r = new StreamReader(gs);
        string confJson = r.ReadToEnd();
        
        ProfileConfig? config;
        try { config = JsonConvert.DeserializeObject<ProfileConfig>(confJson); }
        catch { config = null; }
        return config;
    }
}

public class CondSetConfig
{
    [JsonProperty("name")][DefaultValue("")] public string Name = string.Empty;
    [JsonProperty("conds")][DefaultValue(null)] public List<CondConfig> Conditions = [];
    [JsonIgnore] public bool Checked = false;

    public static string ToBase64(CondSetConfig conf)
    {
        string seri = JsonConvert.SerializeObject(conf);
        var bytes = Encoding.UTF8.GetBytes(seri);
        using var ms = new MemoryStream();
        using (var gs = new GZipStream(ms, CompressionMode.Compress))
            gs.Write(bytes, 0, bytes.Length);
        return Convert.ToBase64String(ms.ToArray());
    }

    public static CondSetConfig? FromBase64(string s)
    {
        var data = Convert.FromBase64String(s);
        using var ms = new MemoryStream(data);
        using var gs = new GZipStream(ms, CompressionMode.Decompress);
        using var r = new StreamReader(gs);
        string confJson = r.ReadToEnd();

        CondSetConfig? config;
        try { config = JsonConvert.DeserializeObject<CondSetConfig>(confJson); }
        catch { config = null; }
        return config;
    }
}

public class CondConfig
{
    [JsonProperty("id")][DefaultValue("Cond")] public string ID = "Cond";
    [JsonProperty("arg")][DefaultValue(0)] public dynamic Arg = 0;
    [JsonProperty("negate")][DefaultValue(false)] public bool Negate = false;
    [JsonProperty("Op")][DefaultValue(ConditionManager.BinaryOperator.AND)] public ConditionManager.BinaryOperator Operator = ConditionManager.BinaryOperator.AND;
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string PluginVersion { get; private set; } = ".init";
    public string GetVersion() => PluginVersion;
    public void UpdateVersion()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        PluginVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        Save();
    }
    public bool CheckVersion() => PluginVersion == Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    [JsonIgnore] public static DirectoryInfo ConfigFolder => DalamudApi.PluginInterface.ConfigDirectory;
    [JsonIgnore] public static FileInfo ConfigFile => DalamudApi.PluginInterface.ConfigFile;
    [JsonIgnore] public string PrevPluginVersion = string.Empty;

    [JsonIgnore] public List<ProfileConfig> ProfileConfigs { get; set; } = [];
    [JsonIgnore] public List<CondSetConfig> ConditionSets { get; set; } = [];
    public bool NoConditionCache = false;

    public List<string> EncodedProfiles = [];
    public List<string> EncodedConditionSets = [];
    public bool ExportOnDelete = false;
    public bool PopulateWhenCreatingProfile = false;
    public bool UsePenumbra = true;

    public void Initialize()
    {
        ProfileConfigs.Clear();
        foreach (string profile in EncodedProfiles)
            ProfileConfigs.Add(ProfileConfig.FromBase64(profile));

        ConditionSets.Clear();
        foreach (string cond in EncodedConditionSets)
            ConditionSets.Add(CondSetConfig.FromBase64(cond));
    }

    public void Save(bool failed = false)
    {
        try
        {
            EncodedProfiles.Clear();
            foreach (ProfileConfig profile in ProfileConfigs)
                EncodedProfiles.Add(ProfileConfig.ToBase64(profile));

            EncodedConditionSets.Clear();
            foreach (CondSetConfig cond in ConditionSets)
                EncodedConditionSets.Add(CondSetConfig.ToBase64(cond));

            DalamudApi.PluginInterface.SavePluginConfig(this);
        }
        catch (Exception ex)
        {
            if (!failed)
            {
                DalamudApi.PluginLog.Error("Failed to save. Trying again...");
                Save(true);
            }
            else
            {
                NotificationManager.Display("Failed to save. Please see /xllog for details.",
                    Dalamud.Interface.ImGuiNotification.NotificationType.Error);
                DalamudApi.PluginLog.Error(ex.ToString());
            }
        }
    }
}
