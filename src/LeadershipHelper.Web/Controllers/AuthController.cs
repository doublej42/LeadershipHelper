using System.Security.Claims;
using LeadershipHelper.Application.Auth;
using LeadershipHelper.Domain.Entities;
using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Web.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeadershipHelper.Web.Controllers;

[Route("auth")]
public sealed class AuthController : Controller
{
    private const string FirstLoginTokenPurpose = "LeadershipHelper.Auth.FirstLoginToken.v1";

    private readonly AppDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly IDataProtector _firstLoginTokenProtector;

    public AuthController(
        AppDbContext dbContext,
        IOtpService otpService,
        IEmailSender emailSender,
        IDataProtectionProvider dataProtectionProvider)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _emailSender = emailSender;
        _firstLoginTokenProtector = dataProtectionProvider.CreateProtector(FirstLoginTokenPurpose);
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
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new
            {
                requiresDisplayName = true,
                firstLoginToken = CreateFirstLoginToken(challenge.Contact),
            });
        }

        await SignInUserAsync(user, cancellationToken);

        return Ok(new { redirectUrl = Url.Action("Index", "Situations") });
    }

    [HttpPost("complete-first-login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFirstLogin(CompleteFirstLoginInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryReadFirstLoginToken(input.FirstLoginToken, out var contact))
        {
            return Unauthorized(new { message = "First login token is invalid or expired." });
        }

        var trimmedDisplayName = input.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedDisplayName))
        {
            return BadRequest(new { message = "Please enter your name." });
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == contact, cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                Email = contact,
                DisplayName = trimmedDisplayName,
            };
            _dbContext.Users.Add(user);
        }
        else if (string.IsNullOrWhiteSpace(user.DisplayName))
        {
            user.DisplayName = trimmedDisplayName;
        }

        await SignInUserAsync(user, cancellationToken);

        return Ok(new { redirectUrl = Url.Action("Index", "Situations") });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private string CreateFirstLoginToken(string contact)
    {
        var expiresUnixSeconds = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        return _firstLoginTokenProtector.Protect($"{contact}\n{expiresUnixSeconds}");
    }

    private bool TryReadFirstLoginToken(string token, out string contact)
    {
        contact = string.Empty;

        try
        {
            var payload = _firstLoginTokenProtector.Unprotect(token);
            var separatorIndex = payload.LastIndexOf('\n');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var email = payload[..separatorIndex];
            var expiry = payload[(separatorIndex + 1)..];

            if (!long.TryParse(expiry, out var expiresUnixSeconds))
            {
                return false;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnixSeconds)
            {
                return false;
            }

            contact = email;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task SignInUserAsync(AppUser user, CancellationToken cancellationToken)
    {
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
    }
}
