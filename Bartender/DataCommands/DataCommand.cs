using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public abstract class DataCommand
{
    public abstract void Execute();

    public abstract void Undo();
}
