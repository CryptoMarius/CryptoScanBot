using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Settings;


[Serializable]
/// Controle hoe een munt zich gedraagt
public class PauseTradingRule
{
    public string Symbol { get; set; } = "";
    public double Percentage { get; set; }
    public int Candles { get; set; }
    public CryptoIntervalPeriod Interval { get; set; }

    // de wachttijd gemeten in minuten
    public int CoolDown { get; set; }
}

public class SettingsTradingBase
{
    // Is deze trade richting actief
    public bool Active { get; set; } = true;

    //***************************
    // Barometer
    // Geen nieuwe posities openen als de barometer onder een van deze getallen staat
    public decimal Barometer15mBotMinimal { get; set; }
    public decimal Barometer30mBotMinimal { get; set; }
    public decimal Barometer01hBotMinimal { get; set; }
    public decimal Barometer04hBotMinimal { get; set; }
    public decimal Barometer24hBotMinimal { get; set; }
}

public class SettingsTradingLong : SettingsTradingBase
{
    public SettingsTradingLong()
    {
        Barometer15mBotMinimal = -0.5m;
        Barometer30mBotMinimal = -0.5m;
        Barometer01hBotMinimal = 0.3m;
        Barometer04hBotMinimal = -1.0m;
        Barometer24hBotMinimal = -99m;
    }
}

public class SettingsTradingShort : SettingsTradingBase
{
    public SettingsTradingShort()
    {
        Barometer15mBotMinimal = +0.5m;
        Barometer30mBotMinimal = +0.5m;
        Barometer01hBotMinimal = -0.3m;
        Barometer04hBotMinimal = +1.0m;
        Barometer24hBotMinimal = +99m;
    }
}

[Serializable]
public class CryptoDcaEntry
{
    // Entry amount multiplier for this DCA step, expressed as a percentage: 100 = invest the same
    // amount again (1x), 200 = invest twice the amount (2x), 400 = invest four times the amount (4x).
    public decimal Factor { get; set; }
    public decimal Percentage { get; set; }
}


[Serializable]
public class CryptoTpEntry
{
    // Share of the position quantity closed at this level, expressed as a percentage: 100 = close
    // the entire (remaining) position, 33 = close 33% of it. The LAST entry in SettingsTrading.TpList
    // always absorbs whatever remains (the "rest"), regardless of the value entered here - it only
    // matters for non-last entries.
    public decimal Factor { get; set; }
    // Profit distance (%) from the break-even price for this TP level
    public decimal Percentage { get; set; }
}


[Serializable]
public class SettingsTrading
{
    // Is de BOT actief
    public bool Active { get; set; } = false;

    [Computed]
    public bool ActiveBackup { get; set; } = false;

    //***************************
    // Account - Positie gerelateerd

    // De 3 account types zijn raar gekozen
    public CryptoTradeVia TradeVia { get; set; } = CryptoTradeVia.PaperTrade;

    /// <summary>
    /// Start capital per traded quote coin for paper trading and the emulator. It is handed out once
    /// per quote coin that has no balance yet - an existing balance is the result of earlier trading
    /// and is never topped up. Use the emulator's reset or PaperAssets.ResetAssets to start over.
    /// </summary>
    public decimal PaperAssetStartCapital { get; set; } = 10000m;

    // Trade via exchange (instelling enkel omdat we nu keuze hebben)
    public bool TradeViaExchange { get; set; } = false;
    // Geen nieuwe posities openen (wel bijkopen voor openstaande posities)
    public bool DisableNewPositions { get; set; } = false;

    // =Overkill in de logging
    public bool LogCanceledOrders { get; set; } = true;

    /// <summary>
    /// How a price is put onto the exchange's tick grid. Quantities are not affected - those always
    /// round down. Default since 22-08-2026 is <see cref="CryptoPriceRounding.AgainstPosition"/>:
    /// long up, short down, so both sides are treated the same and both the unfavourable way.
    /// <para>
    /// The reason for the setting is that the original rule (always down) was the only one that did
    /// NOT treat the two sides the same, and that showed up in the numbers: measured over 50.683
    /// positions of the runs 98-163 the long target landed at 1.78772% against 1.81225% for the
    /// short, on a nominal 1.8%.
    /// </para>
    /// <para>
    /// Set it to <see cref="CryptoPriceRounding.FavourPosition"/> to turn the same rule around, or to
    /// <see cref="CryptoPriceRounding.Down"/> to put everything back exactly as it was before. Per
    /// emulator run it goes in a queue entry as "TradingOverrides": {"PriceRounding": 2}, and it is
    /// captured in the run's settings snapshot, so a finished run says which rule produced it.
    /// </para>
    /// </summary>
    public CryptoPriceRounding PriceRounding { get; set; } = CryptoPriceRounding.AgainstPosition;

    //***************************
    // Slots
    //Maximaal aantal slots voor long en short
    public int SlotsMaximalLong { get; set; } = 1;
    public int SlotsMaximalShort { get; set; } = 1;


    //***************************
    // Entry conditions
    public SettingsEntryConditions EntryConditions { get; set; } = new();


    //***************************
    // Entry
    public CryptoOrderType EntryOrderType { get; set; } = CryptoOrderType.Market;
    // Verwijder de order indien niet na zoveel candles gevuld
    public int EntryRemoveTime { get; set; } = 5;
    // Pullback (in %) applied when EntryOrderPrice == SignalPriceWithPullback. Positive value:
    // long limit goes below SignalPrice by this percentage, short limit goes above. Designed
    // to land the entry inside a zone for zone-style strategies (smc.rejection, dlz.near …).
    public decimal EntryPullbackPercentage { get; set; } = 0.5m;
    // Het afwijkend percentage bij het kopen
    //public decimal GlobalBuyVarying { get; set; } = -0.01m; // verlagen


    //***************************
    // Dca
    public CryptoOrderType DcaOrderType { get; set; } = CryptoOrderType.Limit; // Alway's! but stoplimit when trailing
    // Same role as EntryPullbackPercentage but for DCA orders.
    public decimal DcaPullbackPercentage { get; set; } = 0.5m;

    // Tijd na een buy om niets te doen (om ladders te voorkomen)
    public int GlobalBuyCooldownTime { get; set; } = 30;

    // Minutes after a FILL (entry, dca, take profit or stop loss - LastTradeDate) in which no new
    // signal is created for that symbol. GlobalBuyCooldownTime above holds off OPENING a position;
    // this one holds off creating the signal in the first place.
    //
    // It also removes a base-interval dependency: whether the position had already left
    // PositionList by the time the signal phase ran depended on how far into the candle it closed,
    // which differs per base interval. Asking "did we trade recently" instead does not.
    // Keep it at or above the coarsest base interval used.
    public int SignalCooldownAfterTradeTime { get; set; } = 15;

    // Minutes to stay out of a symbol after a position on it closed at a LOSS, counted from that
    // close. Replaces GlobalBuyCooldownTime for that symbol while it lasts, so it only ever makes
    // the wait longer, never shorter. Zero switches it off, which is the default: the existing two
    // cooldowns keep behaving exactly as before until a value is entered.
    //
    // Only a real loss counts (position.Profit below zero on a Ready position). A position that
    // timed out never bought anything, so it is not a losing trade.
    public int LossCooldownTime { get; set; } = 0;

    // Days a position may stay open before it is closed at whatever the market offers, counted
    // from position.CreateTime. Zero switches it off, which is the default: nothing changes until
    // a value is entered.
    //
    // It exists because the take profit sets no deadline of its own. A wide take profit turns the
    // rare position that never comes back into one that stays open for months - measured on a dbr
    // run with take profit 7.5%: four positions of 2527 ran past 30 days, the longest 64.6 days.
    // They are a small drag (-19.77 USDT on a +511.89 run) but they hold capital that cannot be
    // put to work anywhere else.
    //
    // Fractional values are allowed, so sub-day deadlines can be measured too.
    public decimal MaxPositionDurationDays { get; set; } = 0m;


    //***************************
    // Take profit
    public CryptoOrderType TakeProfitOrderType { get; set; } = CryptoOrderType.Limit;
    public CryptoTakeProfitStrategy TakeProfitStrategy { get; set; } = CryptoTakeProfitStrategy.FixedPercentage;

    // Allow previous (small) dust to be added to the TP
    public bool AddDustToTp { get; set; } = true;
    // Zet een OCO zodra we in de winst zijn (kan het geen verlies trade meer worden, samen met tracing)
    //public bool LockProfits { get; set; } = false;

    //***************************
    // Stop loss
    public decimal StopLossPercentage { get; set; } = 0m;
    public decimal StopLossLimitPercentage { get; set; } = 0m;

    // SL protection (break-even): once an open position reaches MoveSlToBreakEvenPercentage in profit,
    // the stop-loss is pulled up to the break-even price and kept there (sticky, never loosened again).
    // Paper-trade only, same as the rest of the stop-loss handling.
    public bool MoveSlToBreakEven { get; set; } = false;
    public decimal MoveSlToBreakEvenPercentage { get; set; } = 0.5m;


    //***************************
    // Perpetual / Futures
    // De buy en sell leverage (die zijn in alle gevallen gelijk)
    public decimal Leverage { get; set; } = 1m;
    // Cross Of Isolated Margin trading
    public int CrossOrIsolated { get; set; } = 1;


    // Op welke intervallen, strategieën, trend, barometer willen we traden?
    public SettingsTextual Long { get; set; } = new();
    public SettingsTextual Short { get; set; } = new();

    public List<PauseTradingRule> PauseTradingRules { get; set; } = [];

    // Multi-level dca (e.g. 33% at +1%, 33% at +2%, rest at +3%)
    public List<CryptoDcaEntry> DcaList { get; set; } = [];

    // Multi-level take profit (e.g. 33% at +1%, 33% at +2%, rest at +3%). Defaults to a single
    // level (0.75%, 100% of the position).
    public List<CryptoTpEntry> TpList { get; set; } = [new CryptoTpEntry { Percentage = 0.75m, Factor = 100m }];



    public SettingsTrading()
    {
        Long.Barometer.List.Add("1h", (-1.5m, 999m));
        Short.Barometer.List.Add("1h", (-999m, 1.5m));

        Long.IntervalTrend.List.Add("1h");
        Short.IntervalTrend.List.Add("1h");

        //Long.MarketTrend.List.Add((0m, 100m));
        //Short.MarketTrend.List.Add((-100m, 0));

        // No symbol on purpose: the exchange fills it in with its own name for bitcoin (see
        // ExchangeOptions.PauseSymbol). "BTCUSDT" was hardcoded here, which does not exist on
        // Kraken (BTCUSD), Kucoin Perpetual (XBTUSDC) or HyperLiquid Spot (UBTCUSDC), so on those
        // exchanges the rule quietly did nothing. An empty symbol keeps working after switching
        // exchange; filling one in yourself pins the rule to that one coin.
        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "",
            Percentage = 1.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval2m,
            CoolDown = 20,
        });

        PauseTradingRules.Add(new PauseTradingRule()
        {
            Symbol = "",
            Percentage = 2.5,
            Candles = 5,
            Interval = CryptoIntervalPeriod.interval5m,
            CoolDown = 20,
        });


        DcaList.Add(new CryptoDcaEntry()
        {
            Factor = 200m, // 2x the entry amount
            Percentage = 1.5m,
        });
        DcaList.Add(new CryptoDcaEntry()
        {
            Factor = 400m, // 4x the entry amount
            Percentage = 4.5m,
        });
    }

}

