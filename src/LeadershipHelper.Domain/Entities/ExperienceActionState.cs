namespace LeadershipHelper.Domain.Entities;

public sealed class ExperienceActionState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExperienceId { get; set; }
    public Guid SituationActionId { get; set; }
    public bool IsDone { get; set; }
    public string? ResponseText { get; set; }
    public DateTimeOffset LastChangedUtc { get; set; } = DateTimeOffset.UtcNow;

    public Experience? Experience { get; set; }
    public SituationAction? SituationAction { get; set; }
}
