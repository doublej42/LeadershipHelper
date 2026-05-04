using System.Security.Claims;
using LeadershipHelper.Domain.Entities;
using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Web.Models.Situations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace LeadershipHelper.Web.Controllers;

public sealed class SituationsController : Controller
{
    private readonly AppDbContext _dbContext;

    public SituationsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid? TryGetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return value is not null ? Guid.Parse(value) : null;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var query = _dbContext.Situations
            .AsNoTracking()
            .Where(x => x.IsCommunity)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Title.Contains(term) ||
                (_dbContext.Users.Where(u => u.Id == x.CreatorUserId).Select(u => u.DisplayName).FirstOrDefault() ?? x.AuthorName ?? string.Empty).Contains(term));
        }

        var items = await query
            .OrderBy(x => x.Title)
            .Select(x => new SituationListItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = _dbContext.Users.Where(u => u.Id == x.CreatorUserId).Select(u => u.DisplayName).FirstOrDefault() ?? x.AuthorName ?? "Unknown",
                ActionCount = x.Actions.Count,
            })
            .ToListAsync(cancellationToken);

        ViewData["Query"] = q;
        return View(items);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();

        var situation = await _dbContext.Situations
            .AsNoTracking()
            .Include(x => x.Actions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (situation is null)
        {
            return NotFound();
        }

        var isOwner = userId.HasValue && situation.CreatorUserId.HasValue && situation.CreatorUserId.Value == userId.Value;

        // Filter actions by visibility for this viewer
        var visibleActions = situation.Actions
            .Where(a => !a.IsArchived && (
                (a.IsCommunity && a.IsApproved) ||
                (userId.HasValue && a.CreatorUserId == userId) ||
                isOwner))
            .OrderBy(a => a.SortOrder)
            .ToList();

        // Resolve contributor display names for actions added by non-owners
        var contributorIds = visibleActions
            .Where(a => a.CreatorUserId.HasValue && a.CreatorUserId != situation.CreatorUserId)
            .Select(a => a.CreatorUserId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> contributorNames = new();
        if (contributorIds.Count > 0)
        {
            contributorNames = await _dbContext.Users
                .AsNoTracking()
                .Where(u => contributorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? "Unknown", cancellationToken);
        }

        var situationAuthorName = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == situation.CreatorUserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? situation.AuthorName ?? "Unknown";

        var item = new SituationDetailsViewModel
        {
            Id = situation.Id,
            Title = situation.Title,
            AuthorName = situationAuthorName,
            Actions = visibleActions.Select(a => new SituationActionViewModel
            {
                Id = a.Id,
                PromptMarkdown = a.PromptMarkdown,
                RequiresTextResponse = a.RequiresTextResponse,
                ContributorName = a.CreatorUserId.HasValue && a.CreatorUserId != situation.CreatorUserId
                    ? contributorNames.GetValueOrDefault(a.CreatorUserId.Value, "Unknown")
                    : null,
                PendingApproval = a.IsCommunity && !a.IsApproved,
            }).ToList(),
        };

        if (userId is not null)
        {
            var isSaved = await _dbContext.SavedSituations
                .AnyAsync(x => x.UserId == userId && x.SituationId == id, cancellationToken);

            var activeExperienceId = await _dbContext.Experiences
                .Where(x => x.UserId == userId && x.SituationId == id && x.DidHelp == null)
                .OrderByDescending(x => x.ExperienceDateUtc)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            item = item with
            {
                IsSaved = isSaved,
                ActiveExperienceId = activeExperienceId,
                CanEdit = isOwner,
                CanAddActions = !isOwner,
            };
        }

        return View(item);
    }

    // POST /situations/save
    [Authorize]
    [HttpPost("situations/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Guid situationId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var exists = await _dbContext.SavedSituations
            .AnyAsync(x => x.UserId == userId && x.SituationId == situationId, cancellationToken);

        if (!exists)
        {
            _dbContext.SavedSituations.Add(new SavedSituation
            {
                UserId = userId,
                SituationId = situationId,
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id = situationId });
    }

    // POST /situations/unsave
    [Authorize]
    [HttpPost("situations/unsave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsave(Guid situationId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var saved = await _dbContext.SavedSituations
            .SingleOrDefaultAsync(x => x.UserId == userId && x.SituationId == situationId, cancellationToken);

        if (saved is not null)
        {
            _dbContext.SavedSituations.Remove(saved);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id = situationId });
    }

    // GET /situations/saved
    [Authorize]
    [HttpGet("situations/saved")]
    public async Task<IActionResult> Saved(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var savedIds = await _dbContext.SavedSituations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SavedUtc)
            .Select(x => x.SituationId)
            .ToListAsync(cancellationToken);

        var situations = await _dbContext.Situations
            .AsNoTracking()
            .Where(x => savedIds.Contains(x.Id))
            .Select(x => new SituationListItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = _dbContext.Users.Where(u => u.Id == x.CreatorUserId).Select(u => u.DisplayName).FirstOrDefault() ?? x.AuthorName ?? "Unknown",
                ActionCount = x.Actions.Count,
            })
            .ToListAsync(cancellationToken);

        // Preserve saved order
        var ordered = savedIds
            .Select(id => situations.FirstOrDefault(s => s.Id == id))
            .Where(s => s is not null)
            .Cast<SituationListItemViewModel>()
            .ToList();

        return View(ordered);
    }

    // GET /situations/mine
    [Authorize]
    [HttpGet("situations/mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var items = await _dbContext.Situations
            .AsNoTracking()
            .Where(x => x.CreatorUserId == userId)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new SituationListItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = _dbContext.Users.Where(u => u.Id == x.CreatorUserId).Select(u => u.DisplayName).FirstOrDefault() ?? x.AuthorName ?? "Unknown",
                ActionCount = x.Actions.Count,
            })
            .ToListAsync(cancellationToken);

        return View(items);
    }

    // GET /situations/create
    [Authorize]
    [HttpGet("situations/create")]
    public IActionResult Create()
    {
        var model = new SituationInputModel();
        model.Actions.Add(new ActionInputModel { SortOrder = 1 });
        return View(model);
    }

    // POST /situations/create
    [Authorize]
    [HttpPost("situations/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SituationInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(input);

        var userId = TryGetUserId()!.Value;
        var situation = new Situation
        {
            Title = input.Title.Trim(),
            ShortDescription = input.ShortDescription.Trim(),
            IsCommunity = input.IsCommunity,
            CreatorUserId = userId,
        };

        int order = 1;
        foreach (var a in input.Actions.Where(a => !string.IsNullOrWhiteSpace(a.PromptMarkdown)))
        {
            situation.Actions.Add(new SituationAction
            {
                PromptMarkdown = a.PromptMarkdown.Trim(),
                RequiresTextResponse = a.RequiresTextResponse,
                SortOrder = order++,
            });
        }

        _dbContext.Situations.Add(situation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = situation.Id });
    }

    // GET /situations/{id}/edit
    [Authorize]
    [HttpGet("situations/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var situation = await _dbContext.Situations
            .AsNoTracking()
            .Include(x => x.Actions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (situation is null) return NotFound();

        var userId = TryGetUserId()!.Value;
        var isOwner = situation.CreatorUserId.HasValue && situation.CreatorUserId.Value == userId;

        // Resolve contributor names for non-owner actions
        var contributorIds = situation.Actions
            .Where(a => a.CreatorUserId.HasValue && a.CreatorUserId != situation.CreatorUserId)
            .Select(a => a.CreatorUserId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> contributorNames = new();
        if (contributorIds.Count > 0)
        {
            contributorNames = await _dbContext.Users
                .AsNoTracking()
                .Where(u => contributorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? "Unknown", cancellationToken);
        }

        // Owners see all non-archived actions; non-owners see only approved community + their own
        var visibleActions = situation.Actions
            .Where(a => !a.IsArchived && (
                isOwner ||
                (a.IsCommunity && a.IsApproved) ||
                a.CreatorUserId == userId))
            .OrderBy(a => a.SortOrder)
            .ToList();

        var model = new SituationInputModel
        {
            Title = isOwner ? situation.Title : string.Empty,
            ShortDescription = isOwner ? situation.ShortDescription : string.Empty,
            IsCommunity = situation.IsCommunity,
            Actions = visibleActions.Select(a => new ActionInputModel
            {
                Id = a.Id,
                PromptMarkdown = a.PromptMarkdown,
                RequiresTextResponse = a.RequiresTextResponse,
                SortOrder = a.SortOrder,
                IsCommunity = a.IsCommunity,
                PendingApproval = a.IsCommunity && !a.IsApproved,
                ContributorName = a.CreatorUserId.HasValue && a.CreatorUserId != situation.CreatorUserId
                    ? contributorNames.GetValueOrDefault(a.CreatorUserId.Value, "Unknown")
                    : null,
                IsOwnedByCurrentUser = a.CreatorUserId == userId,
            }).ToList(),
        };

        ViewData["SituationId"] = id;
        ViewData["IsOwner"] = isOwner;
        ViewData["SituationTitle"] = situation.Title;
        return View(model);
    }

    // POST /situations/{id}/edit
    [Authorize]
    [HttpPost("situations/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SituationInputModel input, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var situation = await _dbContext.Situations
            .Include(x => x.Actions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (situation is null) return NotFound();

        var isOwner = situation.CreatorUserId.HasValue && situation.CreatorUserId.Value == userId;

        if (!isOwner && !ModelState.IsValid)
        {
            // For non-owners only validate new actions; strip other errors
            ModelState.Clear();
        }



        if (isOwner)
        {
            if (!ModelState.IsValid)
            {
                ViewData["SituationId"] = id;
                ViewData["IsOwner"] = true;
                ViewData["SituationTitle"] = situation.Title;
                return View(input);
            }
            situation.Title = input.Title.Trim();
            situation.ShortDescription = input.ShortDescription.Trim();
            situation.IsCommunity = input.IsCommunity;

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "This situation was updated by someone else. Please reload and try again.");
                ViewData["SituationId"] = id;
                ViewData["IsOwner"] = isOwner;
                ViewData["SituationTitle"] = situation.Title;
                return View(input);
            }
        }

        var originalActionIds = situation.Actions.Select(x => x.Id).ToHashSet();
        var existingById = situation.Actions.ToDictionary(x => x.Id);
        var retainedExistingIds = new HashSet<Guid>();

        var submittedActions = input.Actions
       .Where(a => !string.IsNullOrWhiteSpace(a.PromptMarkdown))
       .ToList();

        // Rebuild owner ordering from submitted sequence.
        for (int i = 0; i < submittedActions.Count; i++)
        {
            var a = submittedActions[i];
            var nextOrder = i + 1;

            if (a.Id.HasValue && existingById.TryGetValue(a.Id.Value, out var existing))
            {
                retainedExistingIds.Add(existing.Id);
                var trimmedPrompt = a.PromptMarkdown.Trim();
                var promptChanged = !string.Equals(existing.PromptMarkdown, trimmedPrompt, StringComparison.Ordinal)
                    || existing.RequiresTextResponse != a.RequiresTextResponse;

                // Users can edit only actions they created.
                if (existing.CreatorUserId == userId)
                {
                    var wasCommunity = existing.IsCommunity;
                    existing.PromptMarkdown = trimmedPrompt;
                    existing.RequiresTextResponse = a.RequiresTextResponse;

                    if (isOwner)
                    {
                        // Situation owner actions are always community and approved.
                        existing.IsCommunity = true;
                        existing.IsApproved = true;
                    }
                    else
                    {
                        existing.IsCommunity = a.IsCommunity;
                        var visibilityChanged = wasCommunity != existing.IsCommunity;

                        if (existing.IsCommunity)
                        {
                            // Any contributor change to a community action requires owner approval again.
                            existing.IsApproved = !(promptChanged || visibilityChanged) && existing.IsApproved;
                        }
                        else
                        {
                            // Personal actions are visible only to creator and do not need approval.
                            existing.IsApproved = true;
                        }
                    }

                    existing.SortOrder = nextOrder;
                }
            }
        }

        // Remove or archive original actions that were omitted from submission.
        var toRemove = situation.Actions.Where(a => a.CreatorUserId == userId)
            .Where(a => originalActionIds.Contains(a.Id) && !retainedExistingIds.Contains(a.Id))
            .ToList();

        foreach (var r in toRemove)
        {
            var hasStates = await _dbContext.ExperienceActionStates
                .AnyAsync(x => x.SituationActionId == r.Id, cancellationToken);
            if (hasStates)
            {
                r.IsArchived = true;
            }
            else
            {
                _dbContext.SituationActions.Remove(r);
            }
        }


        // Append new actions after existing ones.
        int order = situation.Actions.Count > 0 ? situation.Actions.Max(a => a.SortOrder) + 1 : 1;

        var submittedNewActions = input.NewActions
            .Where(a => !string.IsNullOrWhiteSpace(a.PromptMarkdown))
            .ToList();

        foreach (var a in submittedNewActions)
        {
            _dbContext.SituationActions.Add(new SituationAction
            {
                SituationId = situation.Id,
                PromptMarkdown = a.PromptMarkdown.Trim(),
                RequiresTextResponse = a.RequiresTextResponse,
                SortOrder = order++,
                CreatorUserId = userId,
                IsCommunity = isOwner || a.IsCommunity,
                // Owner's own actions are always approved.
                // Non-owner community actions need owner approval; non-community are personal (auto-approved).
                IsApproved = isOwner || !a.IsCommunity,
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Issues adding new actions. This situation was likely updated by someone else. Please reload and try again.");
            ViewData["SituationId"] = id;
            ViewData["IsOwner"] = isOwner;
            ViewData["SituationTitle"] = situation.Title;
            return View(input);
        }

        return RedirectToAction(nameof(Details), new { id = situation.Id });
    }

    // POST /situations/{id}/actions/{actionId}/approve
    [Authorize]
    [HttpPost("situations/{id:guid}/actions/{actionId:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAction(Guid id, Guid actionId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var situation = await _dbContext.Situations
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (situation is null) return NotFound();
        if (!situation.CreatorUserId.HasValue || situation.CreatorUserId.Value != userId) return Forbid();

        var action = await _dbContext.SituationActions
            .SingleOrDefaultAsync(x => x.Id == actionId && x.SituationId == id, cancellationToken);
        if (action is null) return NotFound();

        action.IsApproved = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /situations/{id}/actions/{actionId}/reject
    [Authorize]
    [HttpPost("situations/{id:guid}/actions/{actionId:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectAction(Guid id, Guid actionId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId()!.Value;

        var situation = await _dbContext.Situations
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (situation is null) return NotFound();
        if (!situation.CreatorUserId.HasValue || situation.CreatorUserId.Value != userId) return Forbid();

        var action = await _dbContext.SituationActions
            .SingleOrDefaultAsync(x => x.Id == actionId && x.SituationId == id, cancellationToken);
        if (action is null) return NotFound();

        action.IsCommunity = false;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Edit), new { id });
    }
}
