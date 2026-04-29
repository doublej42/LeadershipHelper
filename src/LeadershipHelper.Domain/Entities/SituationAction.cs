namespace LeadershipHelper.Domain.Entities;

public sealed class SituationAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SituationId { get; set; }
    public string PromptMarkdown { get; set; } = string.Empty;
    public bool RequiresTextResponse { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public Situation? Situation { get; set; }
}
