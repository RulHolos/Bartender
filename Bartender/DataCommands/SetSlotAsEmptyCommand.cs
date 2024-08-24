using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Bartender.DataCommands;

public class SetSlotAsEmptyCommand(ProfileConfig cfg, int profileIndex, int slotIndex) : DataCommand
{
    private ProfileConfig Config { get; set; } = cfg;
    private int ProfileIndex { get; set; } = profileIndex;
    private HotbarSlot OldSlot { get; set; }
    private int SlotIndex { get; set; } = slotIndex;

    public override void Execute()
    {
        HotbarSlot[] hotbar = Config.GetRow(ProfileIndex);
        OldSlot = hotbar[SlotIndex];

        hotbar[SlotIndex] = new HotbarSlot(0, RaptureHotbarModule.HotbarSlotType.Empty, 0, "", OldSlot.Transparent);

        Config.SetRow(hotbar, ProfileIndex);
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        HotbarSlot[] hotbar = Config.GetRow(ProfileIndex);

        hotbar[SlotIndex] = OldSlot;

        Config.SetRow(hotbar, ProfileIndex);
        Bartender.Configuration.Save();
    }

    public override string ToString() => $"Set slot #{ProfileIndex + 1} to empty.";
}
