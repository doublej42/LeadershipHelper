using System.Security.Claims;
using LeadershipHelper.Domain.Entities;
using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Web.Models.Situations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            query = query.Where(x => x.Title.Contains(term) || (x.AuthorName ?? string.Empty).Contains(term));
        }

        var items = await query
            .OrderBy(x => x.Title)
            .Select(x => new SituationListItemViewModel
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = x.AuthorName ?? "Unknown",
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
        var item = await _dbContext.Situations
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SituationDetailsViewModel
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = x.AuthorName ?? "Unknown",
                Actions = x.Actions.OrderBy(a => a.SortOrder).Select(a => a.PromptMarkdown).ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        var userId = TryGetUserId();
        if (userId is not null)
        {
            var isSaved = await _dbContext.SavedSituations
                .AnyAsync(x => x.UserId == userId && x.SituationId == id, cancellationToken);

            var activeExperienceId = await _dbContext.Experiences
                .Where(x => x.UserId == userId && x.SituationId == id && x.DidHelp == null)
                .OrderByDescending(x => x.ExperienceDateUtc)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            item = item with { IsSaved = isSaved, ActiveExperienceId = activeExperienceId };
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
                AuthorName = x.AuthorName ?? "Unknown",
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

        var situation = new Situation
        {
            Title = input.Title.Trim(),
            ShortDescription = input.ShortDescription.Trim(),
            AuthorName = string.IsNullOrWhiteSpace(input.AuthorName) ? null : input.AuthorName.Trim(),
            IsCommunity = true,
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

        var model = new SituationInputModel
        {
            Title = situation.Title,
            ShortDescription = situation.ShortDescription,
            AuthorName = situation.AuthorName,
            Actions = situation.Actions
                .OrderBy(a => a.SortOrder)
                .Select(a => new ActionInputModel
                {
                    Id = a.Id,
                    PromptMarkdown = a.PromptMarkdown,
                    RequiresTextResponse = a.RequiresTextResponse,
                    SortOrder = a.SortOrder,
                })
                .ToList(),
        };

        ViewData["SituationId"] = id;
        return View(model);
    }

    // POST /situations/{id}/edit
    [Authorize]
    [HttpPost("situations/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SituationInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData["SituationId"] = id;
            return View(input);
        }

        var situation = await _dbContext.Situations
            .Include(x => x.Actions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (situation is null) return NotFound();

        situation.Title = input.Title.Trim();
        situation.ShortDescription = input.ShortDescription.Trim();
        situation.AuthorName = string.IsNullOrWhiteSpace(input.AuthorName) ? null : input.AuthorName.Trim();

        var submittedActions = input.Actions
            .Where(a => !string.IsNullOrWhiteSpace(a.PromptMarkdown))
            .ToList();

        // Remove actions not present in submission
        var submittedIds = submittedActions.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();
        var toRemove = situation.Actions.Where(a => !submittedIds.Contains(a.Id)).ToList();
        foreach (var r in toRemove)
            _dbContext.SituationActions.Remove(r);

        int order = 1;
        foreach (var a in submittedActions)
        {
            if (a.Id.HasValue)
            {
                var existing = situation.Actions.SingleOrDefault(x => x.Id == a.Id.Value);
                if (existing is not null)
                {
                    existing.PromptMarkdown = a.PromptMarkdown.Trim();
                    existing.RequiresTextResponse = a.RequiresTextResponse;
                    existing.SortOrder = order;
                }
            }
            else
            {
                situation.Actions.Add(new SituationAction
                {
                    PromptMarkdown = a.PromptMarkdown.Trim(),
                    RequiresTextResponse = a.RequiresTextResponse,
                    SortOrder = order,
                });
            }
            order++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Details), new { id = situation.Id });
    }
}
