using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Experiences;

public sealed class UpdateActionStateInput
{
    [Required]
    public Guid StateId { get; set; }
    public bool IsDone { get; set; }
    public string? ResponseText { get; set; }
}

public sealed class CompleteExperienceInput
{
    [Required]
    public Guid ExperienceId { get; set; }
    public string? UserContext { get; set; }
    public string? DetailsMarkdown { get; set; }
    public bool? DidHelp { get; set; }
}
