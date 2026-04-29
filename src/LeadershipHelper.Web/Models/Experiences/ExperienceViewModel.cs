namespace LeadershipHelper.Web.Models.Experiences;

public sealed class ExperienceViewModel
{
    public Guid Id { get; init; }
    public Guid SituationId { get; init; }
    public string SituationTitle { get; init; } = string.Empty;
    public DateTimeOffset ExperienceDateUtc { get; init; }
    public string? UserContext { get; init; }
    public string? DetailsMarkdown { get; init; }
    public bool? DidHelp { get; init; }
    public IReadOnlyList<ExperienceActionStateViewModel> ActionStates { get; init; } = [];

    public int DoneCount => ActionStates.Count(x => x.IsDone);
    public int TotalCount => ActionStates.Count;
}
