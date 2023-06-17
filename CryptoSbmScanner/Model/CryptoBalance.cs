using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("Balance")]
public class CryptoBalance
{
    [Key]
    public int Id { get; set; }

    //[Required]
    public DateTime EventTime { get; set; }

    //[Required]
    public string Name { get; set; }

    //[Required]
    public Decimal Price { get; set; }

    //[Required]
    // Negatief voor een verkoop
    public Decimal Quantity { get; set; }

    //[Required]
    // De Pice * Quantity
    public Decimal QuoteQuantity { get; set; }
    public Decimal UsdtValue { get; set; }

    public Decimal InvestedQuantity { get; set; }
    public Decimal InvestedValue { get; set; }
}
