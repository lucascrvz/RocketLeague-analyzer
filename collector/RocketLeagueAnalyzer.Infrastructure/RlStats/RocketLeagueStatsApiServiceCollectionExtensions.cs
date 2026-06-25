using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RocketLeagueAnalyzer.Domain.RlStats;

namespace RocketLeagueAnalyzer.Infrastructure.RlStats;

public static class RocketLeagueStatsApiServiceCollectionExtensions
{
    public static IServiceCollection AddRocketLeagueStatsApi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<RocketLeagueStatsApiOptions>(
            configuration.GetSection("RocketLeagueStatsApi")
        );
        services.AddSingleton<IRocketLeagueStatsApiClient, RocketLeagueStatsApiClient>();

        return services;
    }
}
