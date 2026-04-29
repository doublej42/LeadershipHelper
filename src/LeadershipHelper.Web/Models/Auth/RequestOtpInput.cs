using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Auth;

public sealed class RequestOtpInput
{
    [Required]
    [EmailAddress]
    public string Contact { get; set; } = string.Empty;
}
