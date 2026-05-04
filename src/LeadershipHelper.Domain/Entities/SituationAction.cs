namespace LeadershipHelper.Domain.Entities;

public sealed class SituationAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SituationId { get; set; }
    public string PromptMarkdown { get; set; } = string.Empty;
    public bool RequiresTextResponse { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>User who added this prompt. Null for seed data.</summary>
    public Guid? CreatorUserId { get; set; }

    /// <summary>True = visible to all (subject to IsApproved). False = private to CreatorUserId only.</summary>
    public bool IsCommunity { get; set; } = true;

    /// <summary>Owner-approved for community prompts; auto-true for owner's own and private prompts.</summary>
    public bool IsApproved { get; set; } = true;

    /// <summary>Soft-deleted: retains ExperienceActionState history but hidden from new experiences.</summary>
    public bool IsArchived { get; set; }

    public Situation? Situation { get; set; }
}
