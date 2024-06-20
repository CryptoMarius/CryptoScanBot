using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("Balance")]
public class CryptoBalance
{
    [Key]
    public int Id { get; set; }

    //[Required]
    public DateTime EventTime { get; set; }

    //[Required]
    public string Name { get; set; } = "";

    //[Required]
    public decimal Price { get; set; }

    //[Required]
    // Negatief voor een verkoop
    public decimal Quantity { get; set; }

    //[Required]
    // De Pice * Quantity
    public decimal QuoteQuantity { get; set; }
    public decimal UsdtValue { get; set; }

    public decimal InvestedQuantity { get; set; }
    public decimal InvestedValue { get; set; }
}
