using Dalamud.Interface.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.UI;

public static class BackupsUI
{
    public static string SelectedFile = string.Empty;

    public static void Draw(Vector2 iconButtonSize)
    {
        ImGui.BeginGroup();
        {
            /*if (ImGui.Button("Save Manual Backup"))
            {

            }
            ImGui.SameLine();*/
            ImGui.BeginDisabled(SelectedFile == string.Empty || !ImGui.GetIO().KeyShift);
            if (ImGui.Button("Load Backup") && ImGui.GetIO().KeyShift)
            {
                Bartender.Configuration.LoadBackup(new FileInfo(SelectedFile));
            }
            ImGui.EndDisabled();
            ImGuiComponents.HelpMarker(Localization.Get("tooltip.LoadBackup"));

            ImGui.Text($"Selected: {(SelectedFile != string.Empty ? GetReadableDate(SelectedFile) : "...")}");
            if (ImGui.BeginListBox("##Automatic Backups"))
            {
                foreach (string file in Directory.EnumerateFiles(Bartender.Configuration.BackupFolder.FullName).Reverse())
                {
                    if (ImGui.Selectable(GetReadableDate(file), SelectedFile == file))
                    {
                        SelectedFile = file;
                    }
                }
                ImGui.EndListBox();
            }

            if (ImGui.Button("Open Backup Folder"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Bartender.Configuration.BackupFolder.FullName,
                    UseShellExecute = true
                });
            }
        }
        ImGui.EndGroup();
    }

    private static string GetReadableDate(string fileName)
    {
        string[] parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
        string? date = null;
        const string format = "dd-MM-yyyy_HH-mm-ss";
        string displayString = fileName;
        if (parts.Length == 3)
        {
            date = parts[1] + "_" + parts[2];
        }

        if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
        {
            displayString = parsedDate.ToString("dddd dd MMMM, HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return displayString;
    }

    private static void BackupFile(FileInfo file, string name = "", bool overwrite = false)
    {
        try
        {
            if (file.Extension != ".json")
                throw new InvalidOperationException("File must be json!");

            if (string.IsNullOrEmpty(name))
                name = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");

            var path = Bartender.Configuration.GetPluginBackupPath() + $"\\{name}.json";
            file.CopyTo(path, overwrite);
            DalamudApi.PluginLog.Info($"Saved file to {path}");
        }
        catch (Exception e)
        {
            NotificationManager.Display($"Failed to save: {e.Message}", Dalamud.Interface.ImGuiNotification.NotificationType.Error);
        }
    }

    private static void DeleteFile(FileInfo file)
    {
        try
        {
            if (file.Extension != ".json")
                throw new InvalidOperationException("File must be json!");

            file.Delete();
            DalamudApi.PluginLog.Info($"Deleted file {file.FullName}");
        }
        catch (Exception e)
        {
            NotificationManager.Display($"Failed to delete: {e.Message}", Dalamud.Interface.ImGuiNotification.NotificationType.Error);
        }
    }
}
