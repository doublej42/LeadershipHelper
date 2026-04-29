using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Auth;

public sealed class RequestOtpInput
{
    [Required]
    public string Contact { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(email|sms)$", ErrorMessage = "Channel must be email or sms.")]
    public string Channel { get; set; } = "email";
}
