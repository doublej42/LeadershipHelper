using System.Security.Claims;
using LeadershipHelper.Application.Auth;
using LeadershipHelper.Domain.Entities;
using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Web.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeadershipHelper.Web.Controllers;

[Route("auth")]
public sealed class AuthController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;

    public AuthController(AppDbContext dbContext, IOtpService otpService, IEmailSender emailSender)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _emailSender = emailSender;
    }

    [HttpGet("login")]
    public IActionResult Login() => View();

    [HttpPost("request-code")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCode(RequestOtpInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var challenge = _otpService.CreateChallenge();
        _dbContext.OtpChallenges.Add(new OtpChallenge
        {
            Id = challenge.ChallengeId,
            Contact = input.Contact,
            CodeHash = _otpService.HashCode(challenge.Code),
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
            FailedAttempts = 0,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailSender.SendOtpAsync(input.Contact, challenge.Code, cancellationToken);

        return Ok(new { challengeId = challenge.ChallengeId });
    }

    [HttpPost("verify-code")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCode(VerifyOtpInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var challenge = await _dbContext.OtpChallenges
            .SingleOrDefaultAsync(x => x.Id == input.ChallengeId, cancellationToken);

        if (challenge is null || challenge.ConsumedUtc is not null || challenge.ExpiresUtc < DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { message = "Challenge is invalid or expired." });
        }

        if (!_otpService.Verify(input.Code, challenge.CodeHash))
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= 10)
            {
                challenge.ConsumedUtc = DateTimeOffset.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Unauthorized(new { message = "Invalid code." });
        }

        challenge.ConsumedUtc = DateTimeOffset.UtcNow;

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == challenge.Contact, cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                Email = challenge.Contact,
                DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? null : input.DisplayName.Trim(),
            };
            _dbContext.Users.Add(user);
        }
        else if (string.IsNullOrWhiteSpace(user.DisplayName) && !string.IsNullOrWhiteSpace(input.DisplayName))
        {
            user.DisplayName = input.DisplayName.Trim();
        }

        _dbContext.AuthSessions.Add(new AuthSession
        {
            UserId = user.Id,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? "User"),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                AllowRefresh = true,
            });

        return Ok(new { redirectUrl = Url.Action("Index", "Situations") });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
