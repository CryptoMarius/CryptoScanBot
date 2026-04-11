//using CryptoScanner.Core.Context;
//using CryptoScanner.Core.Core;
//using CryptoScanner.Core.Enums;
//using CryptoScanner.Core.Settings;
//using CryptoScanner.Core.Trader;

//using Dapper;

//namespace CryptoScanner.Core.Signal;

///// <summary>
///// Monitors the rolling win rate per (strategy, side) and temporarily blocks strategies
///// that are underperforming in the current market regime.
/////
///// How it works:
/////   1. RefreshAsync() is called periodically (every 15 minutes via ScannerSession).
/////   2. It queries the signal database for closed signals within MaxLookbackDays.
/////   3. For each (strategy, side) combination it calculates the win rate.
/////   4. If win rate is below FeedbackBlockThreshold (and MinSignals is met), the strategy
/////      is blocked until ReEnableHours has elapsed.
/////   5. IsBlocked() is called in SignalExecute for every potential signal — it is a simple
/////      dictionary lookup so it has negligible performance impact.
/////
///// Threading: all cache mutations are protected by a lock. IsBlocked() is safe to call
///// from multiple threads simultaneously.
///// </summary>
//public static class StrategyPerformanceMonitor
//{
//    private sealed class BlockEntry
//    {
//        public decimal WinRate;
//        public int SignalCount;
//        public DateTime? BlockedAt;
//        public DateTime? BlockedUntil;
//    }

//    private static readonly object _lock = new();
//    private static readonly Dictionary<(CryptoSignalStrategy strategy, CryptoTradeSide side), BlockEntry> _cache = [];

//    /// <summary>
//    /// Returns true when the strategy+side combination is currently blocked.
//    /// A block expires automatically after ReEnableHours — the next RefreshAsync will then
//    /// re-evaluate whether to block again.
//    /// </summary>
//    public static bool IsBlocked(CryptoSignalStrategy strategy, CryptoTradeSide side)
//    {
//        var settings = TradingConfig.Signals[side];
//        if (!settings.Feedback.Active)
//            return false;

//        lock (_lock)
//        {
//            if (!_cache.TryGetValue((strategy, side), out BlockEntry? entry))
//                return false;

//            if (!entry.BlockedAt.HasValue)
//                return false;

//            // Auto re-enable: block has expired
//            if (entry.BlockedUntil.HasValue && DateTime.UtcNow >= entry.BlockedUntil.Value)
//            {
//                entry.BlockedAt = null;
//                entry.BlockedUntil = null;
//                return false;
//            }

//            return true;
//        }
//    }

//    /// <summary>
//    /// Returns a snapshot of current performance data for display/logging purposes.
//    /// Key = (strategy, side), Value = (winRate %, signalCount, isBlocked, blockedUntil).
//    /// </summary>
//    public static List<(CryptoSignalStrategy strategy, CryptoTradeSide side, decimal winRate, int count, bool isBlocked, DateTime? blockedUntil)> GetSnapshot()
//    {
//        var result = new List<(CryptoSignalStrategy, CryptoTradeSide, decimal, int, bool, DateTime?)>();
//        lock (_lock)
//        {
//            foreach (var (key, entry) in _cache)
//            {
//                bool blocked = entry.BlockedAt.HasValue &&
//                    (!entry.BlockedUntil.HasValue || DateTime.UtcNow < entry.BlockedUntil.Value);
//                result.Add((key.strategy, key.side, entry.WinRate, entry.SignalCount, blocked, entry.BlockedUntil));
//            }
//        }
//        return result;
//    }

//    /// <summary>
//    /// Queries the database and refreshes the performance cache.
//    /// Should be called periodically (every 15 minutes) and once at startup.
//    /// </summary>
//    public static Task RefreshAsync()
//    {
//        if (GlobalData.ActiveExchange == null)
//            return Task.CompletedTask;

//        var settingsLong = TradingConfig.Signals[CryptoTradeSide.Long];
//        var settingsShort = TradingConfig.Signals[CryptoTradeSide.Short];

//        if (!settingsLong.Feedback.Active && !settingsShort.Feedback.Active)
//            return Task.CompletedTask;

//        // Use the widest lookback window so a single query covers both sides
//        int maxDays = Math.Max(settingsLong.Feedback.MaxDays, settingsShort.Feedback.MaxDays);
//        DateTime fromDate = DateTime.UtcNow.AddDays(-maxDays);

//        try
//        {
//            const string sql = """
//                SELECT Strategy, Side,
//                    COUNT(CASE WHEN SignalStatus = 1 THEN 1 END) AS Wins,
//                    COUNT(CASE WHEN SignalStatus = 2 THEN 1 END) AS Losses
//                FROM Signal
//                WHERE ExchangeId = @exchangeId
//                  AND BackTest = 0
//                  AND SignalStatus IN (1, 2)
//                  AND OpenDate >= @fromDate
//                GROUP BY Strategy, Side
//                HAVING (COUNT(CASE WHEN SignalStatus = 1 THEN 1 END) +
//                        COUNT(CASE WHEN SignalStatus = 2 THEN 1 END)) > 0
//                """;

//            using var database = new CryptoDatabase();
//            var rows = database.Connection.Query(sql, new
//            {
//                exchangeId = GlobalData.ActiveExchange.Id,
//                fromDate
//            });

//            foreach (var row in rows)
//            {
//                var strategy = (CryptoSignalStrategy)(int)row.Strategy;
//                var side = (CryptoTradeSide)(int)row.Side;
//                int wins = (int)row.Wins;
//                int losses = (int)row.Losses;
//                int total = wins + losses;

//                SettingsCompiled settings = TradingConfig.Signals[side];
//                if (!settings.Feedback.Active)
//                    continue;

//                // Not enough data to make a reliable decision
//                if (total < settings.Feedback.MinSignals)
//                    continue;

//                decimal winRate = (decimal)wins / total * 100m;
//                bool shouldBlock = winRate < settings.Feedback.BlockThreshold;

//                lock (_lock)
//                {
//                    if (!_cache.TryGetValue((strategy, side), out BlockEntry? entry))
//                    {
//                        entry = new BlockEntry();
//                        _cache[(strategy, side)] = entry;
//                    }

//                    entry.WinRate = winRate;
//                    entry.SignalCount = total;

//                    // Check if the current block has expired (handle expiry here too, not only in IsBlocked)
//                    if (entry.BlockedAt.HasValue && entry.BlockedUntil.HasValue && DateTime.UtcNow >= entry.BlockedUntil.Value)
//                    {
//                        entry.BlockedAt = null;
//                        entry.BlockedUntil = null;
//                    }

//                    if (shouldBlock && !entry.BlockedAt.HasValue)
//                    {
//                        // New block
//                        entry.BlockedAt = DateTime.UtcNow;
//                        entry.BlockedUntil = DateTime.UtcNow.AddHours(settings.Feedback.ReEnableHours);
//                        if (settings.Feedback.Log)
//                            GlobalData.AddTextToLogTab($"[FeedbackMonitor] {strategy} {side} blocked — win rate {winRate:N1}% over {total} signals (threshold {settings.Feedback.BlockThreshold:N0}%, re-enable after {settings.Feedback.ReEnableHours}h)");
//                    }
//                    else if (!shouldBlock && entry.BlockedAt.HasValue)
//                    {
//                        // Strategy recovered
//                        if (settings.Feedback.Log)
//                            GlobalData.AddTextToLogTab($"[FeedbackMonitor] {strategy} {side} unblocked — win rate {winRate:N1}% over {total} signals");
//                        entry.BlockedAt = null;
//                        entry.BlockedUntil = null;
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            ScannerLog.Logger.Error(ex, "StrategyPerformanceMonitor.RefreshAsync failed");
//            GlobalData.AddTextToLogTab($"[FeedbackMonitor] Refresh error: {ex.Message}");
//        }

//        return Task.CompletedTask;
//    }
//}
