using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("Interval")]
public class CryptoInterval
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public CryptoIntervalPeriod IntervalPeriod { get; set; }

    // Number of seconds for this interval
    public long Duration { get; set; }

    // Verwijzing naar een ander interval waar deze uit op te bouwen is
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