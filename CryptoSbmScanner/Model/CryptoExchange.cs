using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("Exchange")]
public class CryptoExchange
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    [Computed]
    //Datum dat de laatste keer de exchange informatie is opgehaald
    public DateTime ExchangeInfoLastTime { get; set; } = DateTime.MinValue;


    [Computed]
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);

    [Computed]
    public SortedList<string, CryptoAsset> AssetList { get; } = new();

    // De basecoins geindexeerd op id en naam 
    [Computed]
    public SortedList<int, CryptoSymbol> SymbolListId { get; } = new();
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = new();


    // Alle openstaande posities per symbol + locking
    [Computed]
    public SemaphoreSlim PositionListSemaphore { get; set; } = new(1);
    [Computed]
    public SortedList<string, SortedList<int, CryptoPosition>> PositionList { get; } = new();

}