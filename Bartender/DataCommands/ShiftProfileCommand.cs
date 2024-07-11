using Bartender.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ShiftProfileCommand(int ogIndex, int nIndex) : DataCommand
{
    private int originalIndex { get; set; } = ogIndex;
    private int newIndex { get; set; } = nIndex;

    public override void Execute()
    {
        var profile = Bartender.Configuration.ProfileConfigs[originalIndex];
        Bartender.Configuration.ProfileConfigs.RemoveAt(originalIndex);
        Bartender.Configuration.ProfileConfigs.Insert(newIndex, profile);
        Bartender.Configuration.Save();
        ProfileUI.SelectedProfile = profile;
        ProfileUI.SelectedProfileId = newIndex;
    }

    public override void Undo()
    {
        var profile = Bartender.Configuration.ProfileConfigs[newIndex];
        Bartender.Configuration.ProfileConfigs.RemoveAt(newIndex);
        Bartender.Configuration.ProfileConfigs.Insert(originalIndex, profile);
        Bartender.Configuration.Save();
        ProfileUI.SelectedProfile = profile;
        ProfileUI.SelectedProfileId = originalIndex;
    }

    public override string ToString() => $"Shift Profile Indexes [#{originalIndex + 1} --> #{newIndex + 1}]";
}
