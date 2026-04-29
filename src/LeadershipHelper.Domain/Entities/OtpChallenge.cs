namespace LeadershipHelper.Domain.Entities;

public sealed class OtpChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Contact { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresUtc { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? ConsumedUtc { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
