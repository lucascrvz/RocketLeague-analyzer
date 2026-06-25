using System.Buffers;
using System.Text.Json;
using RocketLeagueAnalyzer.Domain.RlStats;

namespace RocketLeagueAnalyzer.Infrastructure.RlStats;

internal static class RlStatsJsonStreamParser
{
    public static List<RlStatsEvent> ParseAvailable(
        ArrayBufferWriter<byte> buffer,
        DateTime receivedAt
    )
    {
        var events = new List<RlStatsEvent>();
        var data = buffer.WrittenSpan;
        var consumed = 0;

        while (consumed < data.Length)
        {
            var reader = new Utf8JsonReader(data.Slice(consumed));

            try
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                consumed += (int)reader.BytesConsumed;

                var root = doc.RootElement;
                if (
                    !root.TryGetProperty("Event", out var eventProp)
                    || eventProp.ValueKind != JsonValueKind.String
                )
                {
                    continue;
                }

                var eventType = eventProp.GetString() ?? string.Empty;
                var payload = string.Empty;

                if (
                    root.TryGetProperty("Data", out var dataProp)
                    && dataProp.ValueKind == JsonValueKind.String
                )
                {
                    payload = dataProp.GetString() ?? string.Empty;
                }

                var matchGuid = ExtractMatchGuid(payload);
                events.Add(new RlStatsEvent(eventType, matchGuid, payload, receivedAt));
            }
            catch (JsonException)
            {
                break;
            }
        }

        if (consumed > 0)
        {
            var remaining = data.Slice(consumed);
            buffer.Clear();
            buffer.Write(remaining);
        }

        return events;
    }

    private static string? ExtractMatchGuid(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (
                doc.RootElement.TryGetProperty("MatchGuid", out var prop)
                && prop.ValueKind == JsonValueKind.String
            )
            {
                return prop.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
