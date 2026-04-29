namespace LeadershipHelper.Web.Models.Experiences;

public sealed class ExperienceActionStateViewModel
{
    public Guid StateId { get; init; }
    public Guid SituationActionId { get; init; }
    public string PromptMarkdown { get; init; } = string.Empty;
    public bool RequiresTextResponse { get; init; }
    public int SortOrder { get; init; }
    public bool IsDone { get; init; }
    public string? ResponseText { get; init; }
}
