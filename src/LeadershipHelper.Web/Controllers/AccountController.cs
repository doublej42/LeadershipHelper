using System.Security.Claims;
using LeadershipHelper.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeadershipHelper.Web.Controllers;

[Authorize]
[Route("account")]
public sealed class AccountController : Controller
{
    private readonly AppDbContext _dbContext;

    public AccountController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == CurrentUserId, cancellationToken);

        if (user is null) return NotFound();

        ViewData["DisplayName"] = user.DisplayName ?? string.Empty;
        ViewData["Email"] = user.Email ?? string.Empty;
        return View();
    }

    [HttpPost("update-name")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateName(string displayName, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == CurrentUserId, cancellationToken);

        if (user is null) return NotFound();

        var trimmed = displayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ModelState.AddModelError(nameof(displayName), "Display name cannot be empty.");
            ViewData["DisplayName"] = displayName ?? string.Empty;
            ViewData["Email"] = user.Email ?? string.Empty;
            return View("Index");
        }

        user.DisplayName = trimmed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Refresh the auth cookie so the navbar name updates immediately
        var principal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                User.Claims.Select(c =>
                    c.Type == System.Security.Claims.ClaimTypes.Name
                        ? new System.Security.Claims.Claim(c.Type, trimmed)
                        : c),
                CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        TempData["Success"] = "Display name updated.";
        return RedirectToAction(nameof(Index));
    }
}
