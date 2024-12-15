using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ShiftConditionCommand(CondSetConfig cfg, CondConfig condc, bool incre) : DataCommand
{
    private CondSetConfig set { get; set; } = cfg;
    private CondConfig cond { get; set; } = condc;
    private bool increment { get; set; } = incre;

    public override void Execute()
    {
        DoTheThing(increment);
    }

    public override void Undo()
    {
        DoTheThing(!increment);
    }

    private void DoTheThing(bool incr)
    {
        var i = set.Conditions.IndexOf(cond);
        if (!incr ? i <= 0 : i >= (set.Conditions.Count - 1)) return;

        var j = (incr ? i + 1 : i - 1);
        var condition = set.Conditions[i];
        set.Conditions.RemoveAt(i);
        set.Conditions.Insert(j, condition);
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Re-arrange Conditions";
}
