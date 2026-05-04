using System.Security.Claims;
using LeadershipHelper.Domain.Entities;
using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Web.Models.Experiences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeadershipHelper.Web.Controllers;

[Authorize]
[Route("experiences")]
public sealed class ExperiencesController : Controller
{
    private readonly AppDbContext _dbContext;

    public ExperiencesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /experiences
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var experiences = await _dbContext.Experiences
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ExperienceDateUtc)
            .Select(x => new ExperienceSummaryViewModel
            {
                Id = x.Id,
                SituationId = x.SituationId,
                SituationTitle = x.Situation!.Title,
                UserContext = x.UserContext,
                ExperienceDateUtc = x.ExperienceDateUtc,
                DidHelp = x.DidHelp,
                DoneCount = x.ActionStates.Count(s => s.IsDone),
                TotalCount = x.ActionStates.Count,
            })
            .ToListAsync(cancellationToken);

        return View(new MyExperiencesViewModel { Experiences = experiences });
    }

    // POST /experiences/start?situationId={id}
    [HttpPost("start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid situationId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        // Fetch the situation and its actions visible to this user:
        // approved community + own personal (non-community), not archived
        var actions = await _dbContext.SituationActions
            .AsNoTracking()
            .Where(x => x.SituationId == situationId &&
                        !x.IsArchived &&
                        ((x.IsCommunity && x.IsApproved) || x.CreatorUserId == userId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (actions.Count == 0)
        {
            return NotFound();
        }

        var experience = new Experience
        {
            UserId = userId,
            SituationId = situationId,
        };
        _dbContext.Experiences.Add(experience);

        foreach (var action in actions)
        {
            _dbContext.ExperienceActionStates.Add(new ExperienceActionState
            {
                ExperienceId = experience.Id,
                SituationActionId = action.Id,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Work), new { id = experience.Id });
    }

    // GET /experiences/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Work(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var experience = await _dbContext.Experiences
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new ExperienceViewModel
            {
                Id = x.Id,
                SituationId = x.SituationId,
                SituationTitle = x.Situation!.Title,
                ExperienceDateUtc = x.ExperienceDateUtc,
                UserContext = x.UserContext,
                DetailsMarkdown = x.DetailsMarkdown,
                DidHelp = x.DidHelp,
                ActionStates = x.ActionStates
                    .OrderBy(s => s.SituationAction!.SortOrder)
                    .Select(s => new ExperienceActionStateViewModel
                    {
                        StateId = s.Id,
                        SituationActionId = s.SituationActionId,
                        PromptMarkdown = s.SituationAction!.PromptMarkdown,
                        RequiresTextResponse = s.SituationAction.RequiresTextResponse,
                        SortOrder = s.SituationAction.SortOrder,
                        IsDone = s.IsDone,
                        ResponseText = s.ResponseText,
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (experience is null)
        {
            return NotFound();
        }

        return View(experience);
    }

    // POST /experiences/update-context
    [HttpPost("update-context")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateContext(Guid experienceId, string? userContext, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var experience = await _dbContext.Experiences
            .SingleOrDefaultAsync(x => x.Id == experienceId && x.UserId == userId, cancellationToken);

        if (experience is null) return NotFound();

        experience.UserContext = string.IsNullOrWhiteSpace(userContext) ? null : userContext.Trim();
        experience.UpdatedUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    // POST /experiences/update-action
    [HttpPost("update-action")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAction(UpdateActionStateInput input, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var state = await _dbContext.ExperienceActionStates
            .Include(x => x.Experience)
            .SingleOrDefaultAsync(x => x.Id == input.StateId && x.Experience!.UserId == userId, cancellationToken);

        if (state is null)
        {
            return NotFound();
        }

        state.IsDone = input.IsDone;
        state.ResponseText = string.IsNullOrWhiteSpace(input.ResponseText) ? null : input.ResponseText.Trim();
        state.LastChangedUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    // POST /experiences/complete
    [HttpPost("complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteExperienceInput input, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var experience = await _dbContext.Experiences
            .SingleOrDefaultAsync(x => x.Id == input.ExperienceId && x.UserId == userId, cancellationToken);

        if (experience is null)
        {
            return NotFound();
        }

        experience.UserContext = string.IsNullOrWhiteSpace(input.UserContext) ? null : input.UserContext.Trim();
        experience.DetailsMarkdown = string.IsNullOrWhiteSpace(input.DetailsMarkdown) ? null : input.DetailsMarkdown.Trim();
        experience.DidHelp = input.DidHelp;
        experience.UpdatedUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    // POST /experiences/{id}/delete
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;

        var experience = await _dbContext.Experiences
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (experience is null)
        {
            return NotFound();
        }

        _dbContext.Experiences.Remove(experience);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
