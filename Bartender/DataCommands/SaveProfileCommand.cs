using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace Bartender.DataCommands;

public class SaveProfileCommand(int id, HotbarSlot[,] nSlots) : DataCommand
{
    private int profileID { get; set; } = id;

    private HotbarSlot[,] previousSlots;
    private HotbarSlot[,] newSlots { get; set; } = nSlots;

    public unsafe override void Execute()
    {
        previousSlots = Bartender.Configuration.ProfileConfigs[profileID].Slots;
        Bartender.Configuration.ProfileConfigs[profileID].Slots = newSlots;
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        Bartender.Configuration.ProfileConfigs[profileID].Slots = previousSlots;
        newSlots = Bartender.Configuration.ProfileConfigs[profileID].Slots;
        Bartender.Configuration.Save();
    }

    public override string ToString() => "Save Profile";
}
