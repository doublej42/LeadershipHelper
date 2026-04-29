namespace LeadershipHelper.Web.Models.Situations;

public sealed class SituationDetailsViewModel
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public IReadOnlyList<string> Actions { get; init; } = [];
}
