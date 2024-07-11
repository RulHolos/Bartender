using Bartender.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ShiftConditionSetCommand(int ogIndex, int nIndex) : DataCommand
{
    private int originalIndex { get; set; } = ogIndex;
    private int newIndex { get; set; } = nIndex;

    public override void Execute()
    {
        // TODO: Shift condition but also change any profiles using the id of the condition set to match the new one.
        // Do this in the undo part too. Don't keep track of the profiles, just change on the fly.

        var set = Bartender.Configuration.ConditionSets[originalIndex];
        Bartender.Configuration.ConditionSets.RemoveAt(originalIndex);
        Bartender.Configuration.ConditionSets.Insert(newIndex, set);
        Bartender.Configuration.Save();
        ConditionSetUI.SelectedSet = set;
        ConditionSetUI.SelectedSetId = newIndex;
    }

    public override void Undo()
    {
        var set = Bartender.Configuration.ConditionSets[newIndex];
        Bartender.Configuration.ConditionSets.RemoveAt(newIndex);
        Bartender.Configuration.ConditionSets.Insert(originalIndex, set);
        Bartender.Configuration.Save();
        ConditionSetUI.SelectedSet = set;
        ConditionSetUI.SelectedSetId = originalIndex;
    }

    public override string ToString() => $"Shift Condition Sets Indexes [#{originalIndex + 1} --> #{newIndex + 1}]";
}
