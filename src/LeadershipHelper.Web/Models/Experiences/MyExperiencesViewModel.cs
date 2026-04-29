namespace LeadershipHelper.Web.Models.Experiences;

public sealed class MyExperiencesViewModel
{
    public IReadOnlyList<ExperienceSummaryViewModel> Experiences { get; init; } = [];
}

public sealed class ExperienceSummaryViewModel
{
    public Guid Id { get; init; }
    public Guid SituationId { get; init; }
    public string SituationTitle { get; init; } = string.Empty;
    public string? UserContext { get; init; }
    public DateTimeOffset ExperienceDateUtc { get; init; }
    public bool? DidHelp { get; init; }
    public int DoneCount { get; init; }
    public int TotalCount { get; init; }
}
