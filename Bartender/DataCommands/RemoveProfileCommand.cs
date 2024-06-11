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
        Bartender.Plugin.UI.selectedProfile = null;
        Bartender.Plugin.UI.Profiles.Last().Dispose();
        Bartender.Plugin.UI.Profiles.Remove(Bartender.Plugin.UI.Profiles.Last());
        Bartender.Configuration.ProfileConfigs.Remove(Bartender.Configuration.ProfileConfigs.Last());
        Bartender.Configuration.Save();
        Bartender.Plugin.UI.RefreshProfilesIndexes();
    }

    public override void Undo()
    {
        Bartender.Configuration.ProfileConfigs.Add(Config);
        Bartender.Plugin.UI.Profiles.Add(new ProfileUI(Bartender.Plugin.UI.Profiles.Count));
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Remove profile [{Config.Name}]";
}
