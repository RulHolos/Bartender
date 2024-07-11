using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Bartender.ProfileConfig;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace Bartender.DataCommands;

public class ChangeUsedBarsCommand(BarNums bars, BarNums toSet, int id) : DataCommand
{
    private BarNums oldBars { get; set; } = bars;
    private BarNums newBars { get; set; } = toSet;
    private int profileId { get; set; } = id;

    public override void Execute()
    {
        Bartender.Configuration.ProfileConfigs[profileId].UsedBars = newBars;
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        Bartender.Configuration.ProfileConfigs[profileId].UsedBars = oldBars;
        Bartender.Configuration.Save();
    }

    public override string ToString() => "Change Used Bars";
}
