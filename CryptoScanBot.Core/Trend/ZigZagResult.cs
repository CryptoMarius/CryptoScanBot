using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

[Serializable]
public class ZigZagResult
{
    public required char PointType { get; set; } // indicates a specific point and type e.g. H or L
    public required decimal Value { get; set; }
    public required CryptoCandle Candle { get; set; }

    // Some call this Strong or Weak instead of Dominant, its the same concept
    public bool Dominant { get; set; } = false;
    public bool IsValid { get; set; } = false;
    public bool Dummy { get; set; } = false;
    public string NiceIntro { get; set; } = ""; // intro before box is interesting (a "jump" into the zone, ==not a small step)

    public decimal? BackupValue { get; set; }
    public CryptoCandle? BackupCandle { get; set; }
    public int? BackupIndex { get; set; }


    // Zone
    //public DateTime OpenTime { get; set; }
    public decimal Top { get; set; }
    public decimal Bottom { get; set; }
    public double Percentage { get; set; }
    public long? CloseDate { get; set; }

    public int PivotIndex { get; set; }

    public void ReusePoint(CryptoCandle candle, decimal value, bool dummy, int pivotIndex)
    {
        // Intention is to reset stuff because we are going to reuse a pivot point, clear the other stuff
        Value = value;
        Candle = candle;
        PivotIndex = pivotIndex;
        if (!dummy)
            Backup();

        // Reset other stuff
        Dominant = false;
        Top = 0;
        Bottom = 0;
        Percentage = 0;
        CloseDate = null;
    }

    public void Backup()
    {
        BackupValue = Value;
        BackupCandle = Candle;
        BackupIndex = PivotIndex;
    }

    public void Restore()
    {
        if (BackupValue != null)
            Value = BackupValue.Value;
        if (BackupCandle != null)
            Candle = BackupCandle;
        if (BackupIndex != null)
            PivotIndex = BackupIndex.Value;
    }
}
