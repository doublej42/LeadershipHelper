using LeadershipHelper.Application.Seed;
using LeadershipHelper.Domain.Entities;
using LeadershipHelper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeadershipHelper.Infrastructure.Seed;

public sealed class SeedDataService
{
    private readonly AppDbContext _dbContext;
    private readonly ILeadershipSeedParser _parser;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        AppDbContext dbContext,
        ILeadershipSeedParser parser,
        IHostEnvironment environment,
        ILogger<SeedDataService> logger)
    {
        _dbContext = dbContext;
        _parser = parser;
        _environment = environment;
        _logger = logger;
    }

    public async Task EnsureSystemUserAsync(CancellationToken cancellationToken)
    {
        const string seedEmail = "leadershiphelper@gametech.ca";

        var seedUser = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Email == seedEmail, cancellationToken);

        if (seedUser is null)
        {
            seedUser = new AppUser
            {
                Email = seedEmail,
                DisplayName = "Leadership Helper",
            };
            _dbContext.Users.Add(seedUser);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created system user '{DisplayName}' ({Email}).", seedUser.DisplayName, seedUser.Email);
        }

        // Back-fill situations that have no owner.
        var orphanedSituationCount = await _dbContext.Situations
            .Where(s => s.CreatorUserId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatorUserId, seedUser.Id), cancellationToken);

        if (orphanedSituationCount > 0)
            _logger.LogInformation("Associated {Count} orphaned situation(s) with system user.", orphanedSituationCount);

        // Back-fill situation actions that have no creator.
        var orphanedActionCount = await _dbContext.SituationActions
            .Where(a => a.CreatorUserId == null)
            .ExecuteUpdateAsync(a => a
                .SetProperty(x => x.CreatorUserId, seedUser.Id)
                .SetProperty(x => x.IsCommunity, true)
                .SetProperty(x => x.IsApproved, true),
                cancellationToken);

        if (orphanedActionCount > 0)
            _logger.LogInformation("Associated {Count} orphaned situation action(s) with system user.", orphanedActionCount);
    }

    public async Task SeedFromLeadershipJourneyAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Situations.AnyAsync(cancellationToken))
        {
            return;
        }

        // EnsureSystemUserAsync runs before this and guarantees the system user exists.
        var seedUser = await _dbContext.Users
            .SingleAsync(u => u.Email == "leadershiphelper@gametech.ca", cancellationToken);

        var markdownPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "LeadershipJourney.md"));
        if (!File.Exists(markdownPath))
        {
            _logger.LogWarning("Seed markdown file not found: {Path}", markdownPath);
            return;
        }

        var markdown = await File.ReadAllTextAsync(markdownPath, cancellationToken);
        var parsed = _parser.ParseSituations(markdown);

        foreach (var item in parsed)
        {
            var situation = new Situation
            {
                Title = item.Title,
                ShortDescription = item.Title,
                AuthorName = "Leadership Helper",
                IsCommunity = true,
                CreatorUserId = seedUser.Id,
                Actions = item.Prompts
                    .Select((prompt, index) => new SituationAction
                    {
                        PromptMarkdown = prompt,
                        RequiresTextResponse = true,
                        SortOrder = index + 1,
                        CreatorUserId = seedUser.Id,
                        IsCommunity = true,
                        IsApproved = true,
                    })
                    .ToList(),
            };

            _dbContext.Situations.Add(situation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
