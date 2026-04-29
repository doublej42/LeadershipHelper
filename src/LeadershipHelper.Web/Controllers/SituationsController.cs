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
}
