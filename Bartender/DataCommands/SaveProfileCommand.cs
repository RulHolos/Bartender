using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace Bartender.DataCommands;

public class SaveProfileCommand : DataCommand
{
    private int profileID;
    private HotbarSlot[,] previousSlots;
    private readonly HotbarSlot[,] newSlots;

    public SaveProfileCommand(int id, HotbarSlot[,] nSlots)
    {
        profileID = id;
        newSlots = (HotbarSlot[,])nSlots.Clone();
    }

    public override void Execute()
    {
        previousSlots = (HotbarSlot[,])Bartender.Configuration.ProfileConfigs[profileID].Slots.Clone();
        Bartender.Configuration.ProfileConfigs[profileID].Slots = (HotbarSlot[,])newSlots.Clone();
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        Bartender.Configuration.ProfileConfigs[profileID].Slots = (HotbarSlot[,])previousSlots.Clone();
        Bartender.Configuration.Save();
    }

    public override string ToString() => "Save Profile";
}
