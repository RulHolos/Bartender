using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ShiftIconCommand(ProfileConfig cfg, HotbarSlot barSlot, bool incr, int proId) : DataCommand
{
    private ProfileConfig config { get; set; } = cfg;
    private HotbarSlot slot { get; set; } = barSlot;
    private bool increment { get; set; } = incr;
    private int profileID { get; set; } = proId;

    public override void Execute()
    {
        DoTheThing(increment);
    }

    public override void Undo()
    {
        DoTheThing(!increment);
    }

    private void DoTheThing(bool increment)
    {
        HotbarSlot[] hotbar = config.GetRow(profileID);
        int i = hotbar.ToList().IndexOf(slot);
        if (!increment ? i > 0 : i < (hotbar.Length - 1))
        {
            int j = (increment ? i + 1 : i - 1);

            HotbarSlot oldSlot = hotbar[i];
            HotbarSlot newSlot = hotbar[j];
            hotbar[i] = newSlot;
            hotbar[j] = oldSlot;

            config.SetRow(hotbar, profileID);
            Bartender.Configuration.Save();
        }
    }

    public override string ToString() => $"Re-arrange Icons";
}
