using System.ComponentModel.DataAnnotations;

namespace LeadershipHelper.Web.Models.Situations;

public sealed class SituationInputModel
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string ShortDescription { get; set; } = string.Empty;

    public bool IsCommunity { get; set; }

    public List<ActionInputModel> Actions { get; set; } = new();

    /// <summary>New actions submitted from the edit form (never carry an Id).</summary>
    public List<ActionInputModel> NewActions { get; set; } = new();
}

public sealed class ActionInputModel
{
    public Guid? Id { get; set; }

    public string PromptMarkdown { get; set; } = string.Empty;

    public bool RequiresTextResponse { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Toggle set by non-owner contributors: true = submit for community approval, false = personal only.</summary>
    public bool IsCommunity { get; set; } = true;

    // ── Display-only (not submitted by the form) ──────────────────────────────
    public string? ContributorName { get; set; }
    public bool PendingApproval { get; set; }
    /// <summary>True when the current user created this prompt and may edit its content.</summary>
    public bool IsOwnedByCurrentUser { get; set; }
}
