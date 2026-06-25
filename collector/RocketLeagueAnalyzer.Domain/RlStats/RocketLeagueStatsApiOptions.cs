namespace RocketLeagueAnalyzer.Domain.RlStats;

public class RocketLeagueStatsApiOptions
{
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 49123;

    public int ReconnectDelayMs { get; set; } = 5000;

    public bool StoreUpdateState { get; set; } = false;
}
