using Bartender.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class AddProfileCommand(ProfileConfig cfg) : DataCommand
{
    public ProfileConfig Config { get; set; } = cfg;

    public override void Execute()
    {
        Bartender.Configuration.ProfileConfigs.Add(Config);
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        ProfileUI.SelectedProfile = null;
        Bartender.Configuration.ProfileConfigs.Remove(Bartender.Configuration.ProfileConfigs.Last());
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Create new Profile";
}
