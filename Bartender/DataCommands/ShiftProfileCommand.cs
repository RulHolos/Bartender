using Bartender.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ShiftProfileCommand(int ogIndex, int nIndex, ProfileUI proUI) : DataCommand
{
    private int originalIndex { get; set; } = ogIndex;
    private int newIndex { get; set; } = nIndex;
    private ProfileUI profileUI { get; set; } = proUI;

    public override void Execute()
    {
        Bartender.Plugin.UI.Profiles.RemoveAt(originalIndex);
        Bartender.Plugin.UI.Profiles.Insert(newIndex, profileUI);

        var profile2 = Bartender.Configuration.ProfileConfigs[originalIndex];
        Bartender.Configuration.ProfileConfigs.RemoveAt(originalIndex);
        Bartender.Configuration.ProfileConfigs.Insert(newIndex, profile2);
        Bartender.Configuration.Save();
        Bartender.Plugin.UI.selectedProfile = profile2;
        Bartender.Plugin.UI.selectedProfileID = newIndex;
        Bartender.Plugin.UI.RefreshProfilesIndexes();
    }

    public override void Undo()
    {
        Bartender.Plugin.UI.Profiles.RemoveAt(newIndex);
        Bartender.Plugin.UI.Profiles.Insert(originalIndex, profileUI);

        var profile2 = Bartender.Configuration.ProfileConfigs[newIndex];
        Bartender.Configuration.ProfileConfigs.RemoveAt(newIndex);
        Bartender.Configuration.ProfileConfigs.Insert(originalIndex, profile2);
        Bartender.Configuration.Save();
        Bartender.Plugin.UI.selectedProfile = profile2;
        Bartender.Plugin.UI.selectedProfileID = originalIndex;
        Bartender.Plugin.UI.RefreshProfilesIndexes();
    }

    public override string ToString() => $"Shift Profile Indexes [#{originalIndex + 1} --> #{newIndex + 1}]";
}
