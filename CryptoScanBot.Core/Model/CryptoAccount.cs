using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("TradeAccount")]
public class CryptoAccount
{
    [Key]
    public int Id { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual required CryptoExchange Exchange { get; set; }

    public CryptoAccountType AccountType { get; set; }

    public bool CanTrade { get; set; }

    // All kind of data for this account
    // (emulator can run simultanious with scanner, paper or exchange trading)
    [Computed]
    public AccountData Data { get; } = new();
    
}
