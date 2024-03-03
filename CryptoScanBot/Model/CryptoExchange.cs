using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Model;

[Table("Exchange")]
public class CryptoExchange
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    // Datum dat de laatste keer de exchange informatie is opgehaald
    public DateTime? LastTimeFetched { get; set; }

    public decimal? FeeRate { get; set; }

    // Coins indexed on id and name
    [Computed]
    public SortedList<int, CryptoSymbol> SymbolListId { get; } = new();
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = new();




    /// <summary>
    /// Clear symbol information (after change of exchange)
    /// </summary>
    public void Clear()
    {
        SymbolListId.Clear();
        SymbolListName.Clear();
    }

}