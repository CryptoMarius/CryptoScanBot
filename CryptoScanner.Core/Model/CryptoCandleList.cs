namespace CryptoScanner.Core.Model;

// More or less thread safe (no need for the expensive ToList())
public class CryptoCandleList : SortedDictionary<CandleTime, CryptoCandle> // experiment via SortedDictionary? SortedList TrimExcess!!
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public CryptoCandle LastCandle { get; private set; } = default;

    //#if DEBUG
    //    // Tel huidige locks
    //    var readCount = _lock.RecursiveReadCount;
    //    var writeCount = _lock.RecursiveWriteCount;
    //    var upgradeCount = _lock.RecursiveUpgradeCount;

    //			if (readCount > 0 || writeCount > 0 || upgradeCount > 0)
    //			{
    //				System.Diagnostics.Debug.WriteLine($"⚠️ Recursive lock detected:");
    //				System.Diagnostics.Debug.WriteLine($"   Read locks: {readCount}");
    //				System.Diagnostics.Debug.WriteLine($"   Write locks: {writeCount}");
    //				System.Diagnostics.Debug.WriteLine($"   Upgrade locks: {upgradeCount}");
    //				System.Diagnostics.Debug.WriteLine(new System.Diagnostics.StackTrace());
    //			}
    //#endif

    public void Lock()
    {
        _lock.EnterWriteLock();
    }

    public void Unlock()
    {
        _lock.ExitWriteLock();
    }

    // Thread-safe indexer: getter uses read lock, setter uses write lock.
    // Without this override, direct assignment (e.g. candles[key] = value) bypasses the lock,
    // causing InvalidOperationException in concurrent enumerators (version mismatch).
    public new CryptoCandle this[CandleTime key]
    {
        get
        {
            _lock.EnterReadLock();
            try { return base[key]; }
            finally { _lock.ExitReadLock(); }
        }
        set
        {
            _lock.EnterWriteLock();
            try
            {
                base[key] = value;
                if (LastCandle.OpenTime == 0 || value.OpenTime >= LastCandle.OpenTime)
                    LastCandle = value;
            }
            finally { _lock.ExitWriteLock(); }
        }
    }

    // Thread-safe TryAdd (SortedDictionary does not have a built-in TryAdd)
    public bool TryAdd(CandleTime key, CryptoCandle value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (base.ContainsKey(key))
                return false;
            base.Add(key, value);
            if (LastCandle.OpenTime == 0 || value.OpenTime >= LastCandle.OpenTime)
                LastCandle = value;
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Thread-safe Add
    public new void Add(CandleTime key, CryptoCandle value)
    {
        _lock.EnterWriteLock();
        try
        {
            base.Add(key, value);
            if (LastCandle.OpenTime == 0 || value.OpenTime >= LastCandle.OpenTime)
                LastCandle = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Thread-safe Remove
    public new bool Remove(CandleTime key)
    {
        _lock.EnterWriteLock();
        try
        {
            bool removed = base.Remove(key);
            if (removed && key == LastCandle.OpenTime)
                LastCandle = default;
            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Thread-safe TryGetValue
    public new bool TryGetValue(CandleTime key, out CryptoCandle value)
    {
        _lock.EnterReadLock();
        try
        {
            return base.TryGetValue(key, out value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe clear
    public new void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            base.Clear();
            LastCandle = default;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Thread-safe access to the first candle (lowest key).
    // Callers must NOT use candleList.Values.First() — that enumerates without the read lock.
    public bool TryGetFirstCandle(out CryptoCandle candle)
    {
        _lock.EnterReadLock();
        try
        {
            using var e = base.GetEnumerator();
            if (e.MoveNext())
            {
                candle = e.Current.Value;
                return true;
            }
            candle = default;
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe access to the last candle (highest key).
    // O(1) — reads the tracked LastCandle field instead of iterating the tree.
    public bool TryGetLastCandle(out CryptoCandle candle)
    {
        _lock.EnterReadLock();
        try
        {
            candle = LastCandle;
            return candle.OpenTime != 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    //// Thread-safe snapshot of the last N candle values, ordered ascending by time.
    //// Use this in preference to candleList.Values.TakeLast(n) — that enumerates the underlying
    //// SortedSet without the read lock and throws InvalidOperationException under concurrent writes.
    //public List<CryptoCandle> GetLastNValues(int n)
    //{
    //    _lock.EnterReadLock();
    //    try
    //    {
    //        int total = base.Count;
    //        int skip = Math.Max(0, total - n);
    //        var result = new List<CryptoCandle>(Math.Min(n, total));
    //        int i = 0;
    //        using var e = base.GetEnumerator();
    //        while (e.MoveNext())
    //        {
    //            if (i >= skip)
    //                result.Add(e.Current.Value);
    //            i++;
    //        }
    //        return result;
    //    }
    //    finally
    //    {
    //        _lock.ExitReadLock();
    //    }
    //}

    // Fast O(n·log m) variant: computes expected keys from LastCandle backward using the
    // interval step size, then does individual dictionary lookups instead of iterating the
    // entire tree. Falls back to the slow path when LastCandle is not set.
    public List<CryptoCandle> GetLastNValues(int n, uint intervalDuration)
    {
        _lock.EnterReadLock();
        try
        {
            if (LastCandle.OpenTime == 0 || base.Count == 0)
                return [];

            int count = Math.Min(n, base.Count);
            var result = new List<CryptoCandle>(count);
            CandleTime time = LastCandle.OpenTime - (uint)(count - 1) * intervalDuration;

            for (int i = 0; i < count; i++)
            {
                if (base.TryGetValue(time, out var candle))
                    result.Add(candle);
                time += intervalDuration;
            }
            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe range lookup: walks from startTime to endTime in intervalDuration steps,
    // collecting all matching candles in a single read-lock acquire/release.
    public List<CryptoCandle> GetRange(CandleTime startTime, CandleTime endTime, uint intervalDuration)
    {
        _lock.EnterReadLock();
        try
        {
            int estimatedCount = (int)((endTime.Minutes - startTime.Minutes) / intervalDuration) + 1;
            var result = new List<CryptoCandle>(estimatedCount);
            for (CandleTime t = startTime; t <= endTime; t += intervalDuration)
            {
                if (base.TryGetValue(t, out var candle))
                    result.Add(candle);
            }
            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Thread-safe snapshot of all entries, ordered by key (ascending, matching SortedDictionary order).
    // Use this instead of direct LINQ enumeration (e.g. .OrderBy/.Select/.ToList) to avoid
    // ArgumentException / InvalidOperationException when another thread calls Add() concurrently.
    //
    // NOTE: new List<>(this) must NOT be used here. The List<T>(ICollection<T>) constructor calls
    // ICollection.Count once for pre-allocation and then SortedSet.CopyTo re-reads Count internally.
    // When Count increases between those two reads (e.g. concurrent Add via a base-type reference),
    // SortedSet.CopyTo throws ArgumentException: "Destination array is not long enough".
    // Enumerating manually avoids both Count reads and the CopyTo path entirely.
    public List<KeyValuePair<CandleTime, CryptoCandle>> GetSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            var snapshot = new List<KeyValuePair<CandleTime, CryptoCandle>>(base.Count);
            using var e = base.GetEnumerator();
            while (e.MoveNext())
                snapshot.Add(e.Current);
            return snapshot;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // Dispose lock
    public void Dispose()
    {
        _lock?.Dispose();
    }
}

//
// For a future Helperclass? (StringStream?)
//
//public void DumpSymbol()
//{
//    //Ter debug van een hardnekig probleem met het tonen van de signal
//    var csv = new StringBuilder();
//    var newLine = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", "OpenTime", "IntervalId", "Open", "High", "Low", "Close", "Volume");
//    csv.AppendLine(newLine);

//    Monitor.Enter(History);
//    try
//    {
//        for (int i = 0; i < History.Count; i++)
//        {
//            CryptoCandle candle = History[i];

//            newLine = string.Format("{0};{1};{2};{3};{4};{5};{6}",
//            candle.Time.ToString(),
//            candle.IntervalId.ToString(),
//            candle.Open.ToString(),
//            candle.High.ToString(),
//            candle.Low.ToString(),
//            candle.Close.ToString(),
//            candle.Volume.ToString());

//            csv.AppendLine(newLine);
//        }
//    }
//    finally
//    {
//        Monitor.Exit(History);
//    }
//    string filename = System.IO.Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location));
//    filename = filename + @"\data\" + Symbol.Exchange.Name + @"\Candles\" + Interval.Name + @"\";
//    System.IO.Directory.CreateDirectory(filename);
//    System.IO.File.WriteAllText(filename + Symbol.Name + "-" + Interval.Name + ".csv", csv.ToString());
//}
