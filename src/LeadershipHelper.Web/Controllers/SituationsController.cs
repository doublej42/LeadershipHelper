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

        return View(item);
    }
}
