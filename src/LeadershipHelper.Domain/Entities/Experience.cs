namespace LeadershipHelper.Domain.Entities;

public sealed class Experience
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid SituationId { get; set; }
    public DateTimeOffset ExperienceDateUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? UserContext { get; set; }
    public string? DetailsMarkdown { get; set; }
    public bool? DidHelp { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public Situation? Situation { get; set; }
    public ICollection<ExperienceActionState> ActionStates { get; set; } = new List<ExperienceActionState>();
}
