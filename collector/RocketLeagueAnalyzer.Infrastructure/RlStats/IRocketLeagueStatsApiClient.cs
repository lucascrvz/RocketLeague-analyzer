using RocketLeagueAnalyzer.Domain.RlStats;

namespace RocketLeagueAnalyzer.Infrastructure.RlStats;

public interface IRocketLeagueStatsApiClient
{
    IAsyncEnumerable<RlStatsEvent> ListenAsync(CancellationToken cancellationToken = default);
}
