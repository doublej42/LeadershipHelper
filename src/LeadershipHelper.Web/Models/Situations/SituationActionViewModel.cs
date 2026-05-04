namespace LeadershipHelper.Web.Models.Situations;

public sealed record SituationActionViewModel
{
    public Guid Id { get; init; }
    public string PromptMarkdown { get; init; } = string.Empty;
    public bool RequiresTextResponse { get; init; }
    /// <summary>Display name of the contributor when different from the situation owner. Null = situation owner added it.</summary>
    public string? ContributorName { get; init; }
    /// <summary>True when this is a community prompt awaiting owner approval.</summary>
    public bool PendingApproval { get; init; }
}
