using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bartender;

public static class Localization
{
    public static readonly string[] SupportedLangCodes = { "en", "fr" };
    private static Dictionary<string, string> LangDict;
    private static Dictionary<string, string> FallbackDict;

    private const string FallbackLangCode = "en";
    private const string LocDirectory = "locals";

    public static string LangCode { get; private set; } = FallbackLangCode;

    public static void Setup(string langCode)
    {
        if (SupportedLangCodes.Contains(langCode.ToLower()))
            LangCode = langCode;
        else
            langCode = FallbackLangCode;

        try
        {
            using (var sr =  new StreamReader(Path.Combine(DalamudApi.PluginInterface.AssemblyLocation.Directory?.FullName!, LocDirectory, $"{LangCode}.json")))
            {
                LangDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
            }

            using (var sr = new StreamReader(Path.Combine(DalamudApi.PluginInterface.AssemblyLocation.Directory?.FullName!, LocDirectory, $"{FallbackLangCode}.json")))
            {
                FallbackDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
            }
        }
        catch (JsonException ex)
        {
            DalamudApi.PluginLog.Error($"Wrong local file format.\n{ex}");
            return;
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Error($"Cannot setup localization.\n{ex}");
            return;
        }
    }

    /// <summary>
    /// Try to get a string with formatting.
    /// </summary>
    /// <param name="key">The string to find</param>
    /// <param name="arguments">Formatting arguments, in order with the formatted string. 0, 1, 2, ...</param>
    /// <returns>The formatted string. Or the key if something went wrong.</returns>
    public static string Get(string key, params object[] arguments)
    {
        try
        {
            if (LangDict.TryGetValue(key, out string s))
                return string.Format(s, arguments);
            else if (FallbackDict.TryGetValue(key, out string s2))
                return string.Format(s2, arguments);
            return key;
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error($"Cannot localize string {key}.\n{e}");
            return key;
        }
    }
}
