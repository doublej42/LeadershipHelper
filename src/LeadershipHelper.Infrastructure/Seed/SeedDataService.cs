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

    public async Task SeedFromLeadershipJourneyAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Situations.AnyAsync(cancellationToken))
        {
            return;
        }

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
                AuthorName = "Leadership Journey",
                IsCommunity = true,
                Actions = item.Prompts
                    .Select((prompt, index) => new SituationAction
                    {
                        PromptMarkdown = prompt,
                        RequiresTextResponse = true,
                        SortOrder = index + 1,
                    })
                    .ToList(),
            };

            _dbContext.Situations.Add(situation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
