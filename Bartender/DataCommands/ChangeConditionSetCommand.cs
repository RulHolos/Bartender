using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ChangeConditionSetCommand(int i1, int i2, ProfileConfig pro) : DataCommand
{
    private ProfileConfig sourceProfile { get; set; } = pro;
    private int originalIndex { get; set; } = i1;
    private int newIndex { get; set; } = i2;

    public override void Execute()
    {
        sourceProfile.ConditionSet = newIndex;
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        sourceProfile.ConditionSet = originalIndex;
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Change Condition Set [#{originalIndex + 1} --> #{newIndex + 1}]";
}
