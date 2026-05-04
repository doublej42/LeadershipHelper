using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeadershipHelper.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task EnsureDatabaseAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedService.EnsureSystemUserAsync(cancellationToken);
        await seedService.SeedFromLeadershipJourneyAsync(cancellationToken);
    }
}
