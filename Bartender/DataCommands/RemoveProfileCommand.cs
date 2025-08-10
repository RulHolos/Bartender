using Bartender.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class RemoveProfileCommand(ProfileConfig cfg) : DataCommand
{
    public ProfileConfig Config { get; set; } = cfg;

    public override void Execute()
    {
        ProfileUI.SelectedProfile = null;
        Bartender.Configuration.ProfileConfigs.Remove(Config);
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        Bartender.Configuration.ProfileConfigs.Add(Config);
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Remove profile [{Config.Name}]";
}
