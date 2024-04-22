using CryptoScanBot.Core.Enums;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("Interval")]
public class CryptoInterval
{
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// Interval name 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Interval enumeration
    /// </summary>
    public CryptoIntervalPeriod IntervalPeriod { get; set; }

    /// <summary>
    /// Number of seconds for this interval
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// Verwijzing naar een ander interval waar deze uit op te bouwen is
    /// </summary>
    public int? ConstructFromId { get; set; }
    [Computed]
    public virtual CryptoInterval ConstructFrom { get; set; }


    public static CryptoInterval CreateInterval(CryptoIntervalPeriod intervalPeriod, string name, long duration, CryptoInterval constructFrom)
    {
        CryptoInterval cryptoInterval = new()
        {
            IntervalPeriod = intervalPeriod,
            Name = name,
            Duration = duration,
            ConstructFrom = constructFrom
        };
        return cryptoInterval;
    }
}