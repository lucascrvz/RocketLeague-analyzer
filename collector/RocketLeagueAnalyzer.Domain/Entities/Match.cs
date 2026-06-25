namespace RocketLeagueAnalyzer.Domain.Entities;

public class Match
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
