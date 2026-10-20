using System.Text.Json;

namespace CryptoScanner.Core.Context;

/// <summary>
/// The unrealised PnL of an emulator run split over long and short, for runs written before
/// database version 108 stored it per side.
/// <para>
/// The run's total unrealised PnL is known for every run with an equity curve: the change of the
/// curve minus the realised profit. Which side it belongs to follows from the position digest,
/// which marks every position still open at the end (status 1) with its side. When the open
/// positions are all on one side, that side has the whole amount and the other side nothing - the
/// exact numbers FinishRun stores. When both sides still have open positions the split needs the
/// quantity of each position, which the digest does not keep, so those runs stay empty.
/// </para>
/// </summary>
public static class RunUnrealizedSplit
{
    private const int StatusOpen = 1;


    /// <summary>
    /// The unrealised PnL per side, or null when it cannot be told apart (open positions on both
    /// sides, or open positions without a digest to say which side they are on).
    /// </summary>
    /// <param name="digest">The position digest as stored on the run row, compressed or not.</param>
    /// <param name="positionsOpen">The run's PositionsOpen column.</param>
    /// <param name="unrealized">The equity change over the run minus the realised profit.</param>
    public static (decimal Long, decimal Short)? Split(string? digest, int positionsOpen, decimal unrealized)
    {
        string? json = PositionDigest.Unpack(digest);
        if (string.IsNullOrEmpty(json))
        {
            if (positionsOpen == 0)
                return (0m, 0m);
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("cols", out JsonElement cols) || !root.TryGetProperty("rows", out JsonElement rows))
            return null;

        List<string?> names = [.. cols.EnumerateArray().Select(c => c.GetString())];
        int sideAt = names.IndexOf("side"), statusAt = names.IndexOf("status"), investedAt = names.IndexOf("invested");
        if (sideAt < 0 || statusAt < 0 || investedAt < 0)
            return null;

        // Only positions that bought something carry value, the same rule as the open-position
        // valuation of FinishRun (Invested > 0): an entry order that never filled has nothing open.
        bool openLong = false, openShort = false;
        foreach (JsonElement row in rows.EnumerateArray())
        {
            if (Value(row, statusAt) != StatusOpen || !(Value(row, investedAt) > 0))
                continue;
            if (Value(row, sideAt) == (int)Enums.CryptoTradeSide.Long)
                openLong = true;
            else
                openShort = true;
        }

        if (openLong && openShort)
            return null;
        if (openLong)
            return (unrealized, 0m);
        if (openShort)
            return (0m, unrealized);
        return (0m, 0m);
    }


    private static double? Value(JsonElement row, int index)
    {
        if (index >= row.GetArrayLength())
            return null;
        JsonElement value = row[index];
        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
    }
}
