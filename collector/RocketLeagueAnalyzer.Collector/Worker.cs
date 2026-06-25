using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RocketLeagueAnalyzer.Domain.Entities;
using RocketLeagueAnalyzer.Domain.RlStats;
using RocketLeagueAnalyzer.Infrastructure.RlStats;
using RocketLeagueAnalyzer.Persistance.Data;

namespace RocketLeagueAnalyzer.Collector;

public class Worker : BackgroundService
{
    private static readonly string[] MatchEndEvents = ["MatchEnded", "MatchDestroyed"];

    private readonly IRocketLeagueStatsApiClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RocketLeagueStatsApiOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IRocketLeagueStatsApiClient client,
        IServiceScopeFactory scopeFactory,
        IOptions<RocketLeagueStatsApiOptions> options,
        ILogger<Worker> logger
    )
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Listening for Rocket League events on {Host}:{Port}",
            _options.Host,
            _options.Port
        );

        await foreach (var evt in _client.ListenAsync(stoppingToken))
        {
            if (!ShouldStore(evt))
            {
                continue;
            }

            if (!TryParseMatchId(evt.MatchGuid, out var matchId))
            {
                _logger.LogDebug("Skipping {EventType}: no MatchGuid in payload", evt.EventType);
                continue;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await EnsureMatchAsync(db, matchId, evt, stoppingToken);
            await PersistEventAsync(db, matchId, evt, stoppingToken);
            await HandleMatchEndAsync(db, matchId, evt, stoppingToken);

            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Stored {EventType} for match {MatchId}",
                evt.EventType,
                matchId
            );
        }
    }

    private bool ShouldStore(RlStatsEvent evt)
    {
        if (evt.EventType == "UpdateState" && !_options.StoreUpdateState)
        {
            return false;
        }

        return true;
    }

    private static bool TryParseMatchId(string? matchGuid, out Guid matchId)
    {
        if (matchGuid is not null && Guid.TryParse(matchGuid, out matchId))
        {
            return true;
        }

        matchId = default;
        return false;
    }

    private static async Task EnsureMatchAsync(
        AppDbContext db,
        Guid matchId,
        RlStatsEvent evt,
        CancellationToken cancellationToken
    )
    {
        var exists = await db.Matches.AnyAsync(m => m.Id == matchId, cancellationToken);
        if (!exists)
        {
            db.Matches.Add(new Match { Id = matchId, StartedAt = evt.ReceivedAt });
        }
    }

    private static Task PersistEventAsync(
        AppDbContext db,
        Guid matchId,
        RlStatsEvent evt,
        CancellationToken cancellationToken
    )
    {
        db.MatchEvents.Add(
            new MatchEvent
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                EventType = evt.EventType,
                OccurredAt = evt.ReceivedAt,
                Payload = evt.Payload,
            }
        );

        return Task.CompletedTask;
    }

    private static async Task HandleMatchEndAsync(
        AppDbContext db,
        Guid matchId,
        RlStatsEvent evt,
        CancellationToken cancellationToken
    )
    {
        if (!MatchEndEvents.Contains(evt.EventType))
        {
            return;
        }

        var match = await db.Matches.FindAsync([matchId], cancellationToken);
        if (match is not null && match.EndedAt is null)
        {
            match.EndedAt = evt.ReceivedAt;
        }
    }
}
