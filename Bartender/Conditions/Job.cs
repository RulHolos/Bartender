using ImGuiNET;

namespace Bartender.Conditions;

public class JobCondition : ICondition, IDrawableCondition, IArgCondition, IConditionCategory
{
    public string ID => "j";
    public string ConditionName => "Job";
    public string CategoryName => "Job";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => DalamudApi.ClientState.LocalPlayer is { } player && player.ClassJob.RowId == (uint)arg;
    public string GetTooltip(CondConfig cfg) => null;
    public string GetSelectableTooltip(CondConfig cfg) => null;
    public void Draw(CondConfig cfg)
    {
        var jobs = DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>();
        if (jobs == null) return;

        var r = jobs.GetRow((uint)cfg.Arg);
        if (!ImGui.BeginCombo("##Job", r.Abbreviation.ToString())) return;

        foreach (var job in jobs)
        {
            if (job.RowId == 0) continue;
            if (!ImGui.Selectable(job.Abbreviation.ToString(), job.RowId == cfg.Arg)) continue;

            cfg.Arg = job.RowId;
            Bartender.Configuration.Save();
        }
        ImGui.EndCombo();
    }
    public dynamic GetDefaultArg(CondConfig cfg) => DalamudApi.ClientState.LocalPlayer is { } player ? player.ClassJob.RowId : 0;
}
