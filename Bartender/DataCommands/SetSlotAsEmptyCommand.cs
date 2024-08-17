using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class SetSlotAsEmptyCommand(ProfileConfig cfg, HotbarSlot barSlot, int proId) : DataCommand
{
    private ProfileConfig config { get; set; } = cfg;
    private HotbarSlot slot { get; set; } = barSlot;
    private int profileID { get; set; } = proId;
    private int slotID { get; set; }

    public override void Execute()
    {
        HotbarSlot[] hotbar = config.GetRow(profileID);
        int i = hotbar.ToList().IndexOf(slot);
        slotID = i;

        hotbar[i] = new HotbarSlot(0, RaptureHotbarModule.HotbarSlotType.Empty, 0, "", slot.Transparent);

        config.SetRow(hotbar, profileID);
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        HotbarSlot[] hotbar = config.GetRow(profileID);

        hotbar[slotID] = slot;

        config.SetRow(hotbar, profileID);
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Set slot #{profileID + 1} to empty.";
}
