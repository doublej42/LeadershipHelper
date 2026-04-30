using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Situations;

public sealed class SituationInputModel
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string ShortDescription { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AuthorName { get; set; }

    public bool IsCommunity { get; set; }

    public List<ActionInputModel> Actions { get; set; } = new();
}

public sealed class ActionInputModel
{
    public Guid? Id { get; set; }

    [Required]
    public string PromptMarkdown { get; set; } = string.Empty;

    public bool RequiresTextResponse { get; set; }

    public int SortOrder { get; set; }
}
