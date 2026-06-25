using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RocketLeagueAnalyzer.Domain.RlStats;

namespace RocketLeagueAnalyzer.Infrastructure.RlStats;

public sealed class RocketLeagueStatsApiClient : IRocketLeagueStatsApiClient
{
    private readonly RocketLeagueStatsApiOptions _options;
    private readonly ILogger<RocketLeagueStatsApiClient> _logger;

    public RocketLeagueStatsApiClient(
        IOptions<RocketLeagueStatsApiOptions> options,
        ILogger<RocketLeagueStatsApiClient> logger
    )
    {
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<RlStatsEvent> ListenAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var channel = Channel.CreateUnbounded<RlStatsEvent>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }
        );

        using var registration = cancellationToken.Register(() =>
            channel.Writer.TryComplete()
        );

        var connectionTask = RunConnectionLoopAsync(channel.Writer, cancellationToken);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            channel.Writer.TryComplete();
            try
            {
                await connectionTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task RunConnectionLoopAsync(
        ChannelWriter<RlStatsEvent> writer,
        CancellationToken cancellationToken
    )
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient? client = null;

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Connected to Rocket League Stats API at {Host}:{Port}",
                    _options.Host,
                    _options.Port
                );

                await ReadStreamAsync(client.GetStream(), writer, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is SocketException or IOException or TimeoutException)
            {
                _logger.LogWarning(
                    ex,
                    "Stats API connection lost, reconnecting in {Delay}ms",
                    _options.ReconnectDelayMs
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading from Stats API");
            }
            finally
            {
                client?.Dispose();
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.ReconnectDelayMs, cancellationToken);
            }
        }

        writer.TryComplete();
    }

    private async Task ReadStreamAsync(
        NetworkStream stream,
        ChannelWriter<RlStatsEvent> writer,
        CancellationToken cancellationToken
    )
    {
        var readBuffer = new byte[4096];
        var accumulator = new ArrayBufferWriter<byte>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            var receivedAt = DateTime.UtcNow;
            accumulator.Write(readBuffer.AsSpan(0, bytesRead));

            foreach (
                var evt in RlStatsJsonStreamParser.ParseAvailable(accumulator, receivedAt)
            )
            {
                await writer.WriteAsync(evt, cancellationToken);
            }
        }
    }
}
