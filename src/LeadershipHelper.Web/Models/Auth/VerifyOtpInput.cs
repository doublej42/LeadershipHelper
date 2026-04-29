using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Auth;

public sealed class VerifyOtpInput
{
    [Required]
    public Guid ChallengeId { get; set; }

    [Required]
    [RegularExpression("^\\d{6}$")]
    public string Code { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
