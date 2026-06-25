namespace RocketLeagueAnalyzer.Domain.RlStats;

public record RlStatsEvent(
    string EventType,
    string? MatchGuid,
    string Payload,
    DateTime ReceivedAt
);
