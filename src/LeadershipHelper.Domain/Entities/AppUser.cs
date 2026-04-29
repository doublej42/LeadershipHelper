namespace LeadershipHelper.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
