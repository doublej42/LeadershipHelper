using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Auth;

public sealed class CompleteFirstLoginInput
{
    [Required]
    public string FirstLoginToken { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}
