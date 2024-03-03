using CryptoScanBot.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Model;

[Table("TradeAccount")]
public class CryptoTradeAccount
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Short { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public CryptoAccountType AccountType { get; set; }
    public CryptoTradeAccountType TradeAccountType { get; set; }

    public bool CanTrade { get; set; }

    // Instellingen:
    // Eventueel aangepaste instellingen per accountType voor bijvoorbeeld backtest project?
    //[Computed]
    // public ConfigWhatever Config { get; set; } 

    // Assets + locking
    [Computed]
    public SemaphoreSlim AssetListSemaphore { get; set; } = new(1);
    [Computed]
    public SortedList<string, CryptoAsset> AssetList { get; } = [];

    // Trades + locking
    //[Computed]
    //public SemaphoreSlim TradeListSemaphore { get; set; } = new(1);
    //[Computed]
    //public SortedList<string, SortedList<int, CryptoTrade>> TradeList { get; set; } = [];

    // Posities + locking
    //[Computed]
    //public SemaphoreSlim PositionListSemaphore { get; set; } = new(1);
    [Computed]
    public SortedList<string, SortedList<int, CryptoPosition>> PositionList { get; } = [];


    /// <summary>
    /// Clear symbol information (after change of exchange)
    /// </summary>
    public void Clear()
    {
        AssetList.Clear();
        //OrderList.Clear();
        PositionList.Clear();
    }

}
