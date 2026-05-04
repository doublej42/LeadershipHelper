namespace LeadershipHelper.Web.Models.Situations;

public sealed record SituationDetailsViewModel
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public IReadOnlyList<SituationActionViewModel> Actions { get; init; } = [];
    // Set by controller when user is authenticated
    public bool IsSaved { get; init; }
    public Guid? ActiveExperienceId { get; init; }
    public bool CanEdit { get; init; }
    public bool CanAddActions { get; init; }
}
