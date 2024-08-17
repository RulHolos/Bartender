using Bartender.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.DataCommands;

public class ToggleSlotTransparencyCommand(ProfileConfig cfg, HotbarSlot barSlot, int proId) : DataCommand
{
    private ProfileConfig config { get; set; } = cfg;
    private HotbarSlot slot { get; set; } = barSlot;
    private int profileID { get; set; } = proId;
    private int slotID { get; set; }

    public override void Execute()
    {
        HotbarSlot[] hotbar = config.GetRow(profileID);
        slotID = hotbar.ToList().IndexOf(slot);

        hotbar[slotID].Transparent = !hotbar[slotID].Transparent;

        config.SetRow(hotbar, profileID);
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        HotbarSlot[] hotbar = config.GetRow(profileID);

        hotbar[slotID].Transparent = !hotbar[slotID].Transparent;

        config.SetRow(hotbar, profileID);
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Toggle slot transparency on [{config.Name}#bar {profileID + 1}]";
}
