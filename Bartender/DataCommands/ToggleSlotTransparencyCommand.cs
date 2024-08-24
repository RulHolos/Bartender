namespace Bartender.DataCommands;

public class ToggleSlotTransparencyCommand(ProfileConfig cfg, int profileIndex, int slotIndex) : DataCommand
{
    private ProfileConfig Config { get; set; } = cfg;
    private int ProfileIndex { get; set; } = profileIndex;
    private int SlotIndex { get; set; } = slotIndex;

    public override void Execute()
    {
        HotbarSlot[] hotbar = Config.GetRow(ProfileIndex);

        hotbar[SlotIndex].Transparent = !hotbar[SlotIndex].Transparent;

        Config.SetRow(hotbar, ProfileIndex);
        Bartender.Configuration.Save();
    }

    public override void Undo()
    {
        // Toggling is trivially undoable
        Execute();
    }

    public override string ToString() => $"Toggle slot transparency on [{Config.Name}#bar {ProfileIndex + 1}]";
}
