namespace LeadershipHelper.Domain.Entities;

public sealed class AuthSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
