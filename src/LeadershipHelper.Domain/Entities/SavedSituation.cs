namespace LeadershipHelper.Domain.Entities;

public sealed class SavedSituation
{
    public Guid UserId { get; set; }
    public Guid SituationId { get; set; }
    public DateTimeOffset SavedUtc { get; set; } = DateTimeOffset.UtcNow;
}
