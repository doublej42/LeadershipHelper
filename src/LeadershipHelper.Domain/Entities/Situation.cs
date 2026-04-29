namespace LeadershipHelper.Domain.Entities;

public sealed class Situation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public bool IsCommunity { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SituationAction> Actions { get; set; } = new List<SituationAction>();
}
