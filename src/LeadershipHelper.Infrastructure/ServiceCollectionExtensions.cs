using LeadershipHelper.Application.Auth;
using LeadershipHelper.Application.Seed;
using LeadershipHelper.Infrastructure.Email;
using LeadershipHelper.Infrastructure.Persistence;
using LeadershipHelper.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeadershipHelper.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ILeadershipSeedParser, LeadershipSeedParser>();
        services.AddScoped<SeedDataService>();

        services.Configure<AzureEmailOptions>(
            configuration.GetSection(AzureEmailOptions.SectionName));
        services.AddScoped<IEmailSender, AzureEmailSender>();

        return services;
    }
}
