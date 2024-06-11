using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class SaveProfileCommand() : DataCommand
{
    public override void Execute()
    {
        return;
    }

    public override void Undo()
    {
        return;
    }

    public override string ToString() => "Save Profile";
}
