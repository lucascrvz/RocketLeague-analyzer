namespace RocketLeagueAnalyzer.Domain.Entities;

public class MatchEvent
{
    public Guid Id { get; set; }

    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public string EventType { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public string Payload { get; set; } = string.Empty;
}
