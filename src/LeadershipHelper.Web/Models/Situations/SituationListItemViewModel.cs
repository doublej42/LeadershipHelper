namespace LeadershipHelper.Web.Models.Situations;

public sealed class SituationListItemViewModel
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public int ActionCount { get; init; }
}
