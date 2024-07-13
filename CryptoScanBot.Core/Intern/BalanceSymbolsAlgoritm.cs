using System.Text;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;

using Dapper.Contrib.Extensions;


namespace CryptoScanBot.Core.Intern;

#if BALANCING
// TODO: Deze Balancing bot heeft een herontwerp en de nodige aandacht nodig!
// In principe willen we (een soort van) mandjes of groepjes oid introduceren.
// Ook zal er een extra API key bij moeten omdat het meestal vanaf een subaccount wordt gedaan
// En de configuratie moet verbeterd worden (ehh, opgezet worden?)


/// <summary>
/// Interne class voor de inventarisatie van balancering
/// </summary>
public class BasketAsset
{
    public string Base { get; set; } //De basismunt (BTC, ETH, USDT enzovoort), enkel voor de rapportage in de Dump()
    public string Quote { get; set; } // Het 2e item Base/Quote
    public CryptoSymbol Symbol { get; set; } // Referentie naar de te kopen of verkopen symbol (behalve de USDT inleg)
    public decimal Percentage { get; set; } // de te gebruiken verdeling
    public decimal Price { get; set; } // de actuele marktprijs 

    // Verschil
    public decimal DiffQuantity { get; set; } // Verschill
    public decimal DiffPercentage { get; set; } // Verschill
    public decimal DiffQuoteQuantity { get; set; } // Verschill

    // Voor de statistiek
    public decimal Quantity { get; set; } // de hoeveelheid muntjes
    public decimal QuoteQuantity { get; set; } // de actuele "USDT" prijs

    // Om de berekeningen te controleren
    public decimal WantedValue { get; set; }
    public decimal WantedQuantity { get; set; }

    public decimal ValueUsdt { get; set; } // de actuele "USDT" prijs
}


public class BalanceSymbolsAlgoritm
{
    private static readonly string useUsdQuote = "ETH";

    // Het te gebruiken basismunt en interval in deze backtest
    //private IntervalPeriod TestInterval = IntervalPeriod.interval1h;
    public static string LastOverviewMessage= "";
    public static string LastOverviewMessageCsv = "";
    public static string LastOverviewMessageCsvShort = "";

    private static string LastAdviceMessage = "";

    // Scheelt een bak met parameters
    private Model.CryptoExchange Exchange = null;
    private DateTime CurrentDate = DateTime.UtcNow;

    // Hoeft alleen public te zijn in testmode (maar hoe doe je dat nu weer?)
    public BasketAsset BasketMain = null;
    public List<BasketAsset> Basket = new();

    // Alleen relevant bij testen
    private readonly bool Simulation = false;
    public int TradeCount = 0;
    public decimal TotalValue = 0;
    public CryptoTradeAccount TradeAccount;
    //public decimal MainQuantity = 0;


    public BalanceSymbolsAlgoritm(CryptoTradeAccount tradeAccount, bool simulation = false)
    {
        Simulation = simulation;
        TradeAccount = tradeAccount;
    }


    private static bool ValidBarometer(BasketAsset balanceAsset, decimal minimalBarometer)
    {
        // Een valide barometer?
        if (minimalBarometer > -999)
        {
            if (!GlobalData.Settings.QuoteCoins.TryGetValue(balanceAsset.Quote, out CryptoQuoteData quoteData))
                return false;

            // We gaan ervan uit dat alles in 1x wordt berekend
            BarometerData barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
            if (!barometerData.PriceBarometer.HasValue)
                return false;

            barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
            if (barometerData.PriceBarometer <= minimalBarometer)
                return false;
        }

        return true;
    }


    private void DumpBasketInfo()
    {
        string p = "";
        string s = "";
        decimal totalQuoteValue = 0;

        foreach (BasketAsset basketAsset in Basket)
        {
            totalQuoteValue += basketAsset.QuoteQuantity;
            s += string.Format("{0} {1} {2} {3} ({4:N2})%\r\n", basketAsset.Base, basketAsset.Quantity.ToString0(),
                basketAsset.QuoteQuantity.ToString0(), basketAsset.DiffQuoteQuantity.ToString0(), basketAsset.DiffPercentage);
        }


        if (GlobalData.Settings.BalanceBot.StartAmount > 0)
        {
            decimal profitValue = totalQuoteValue - GlobalData.Settings.BalanceBot.StartAmount;
            decimal profitPercentage = 100 * profitValue / GlobalData.Settings.BalanceBot.StartAmount;
            p = string.Format("({0:N2})%", profitPercentage);
        }


        LastOverviewMessage = string.Format("{0}\r\nTotaal: {1:N8} {2}\r\n{3}", CurrentDate.ToLocalTime(), totalQuoteValue, p, s);



        StringBuilder b = new();

        string t = string.Format("Symbol;" +
            "Price;Quantity;QuoteQuantity;USDT-Quantity;" +
            "WantedValue;WantedQuantity;" +
            "DiffQuantity;DiffQuoteQuantity;DiffPercentage;Usdt;Usdt2");
        b.AppendLine(t);


        foreach (BasketAsset basketAsset in Basket)
        {
            t = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9:N2};{10};{11:N2}",
                basketAsset.Base,

                basketAsset.Price.ToString0(),
                basketAsset.Quantity.ToString0(),
                basketAsset.QuoteQuantity.ToString0(),
                basketAsset.ValueUsdt.ToString0(),

                basketAsset.WantedValue.ToString0(),
                basketAsset.WantedQuantity.ToString0(),

                basketAsset.DiffQuantity.ToString0(),
                basketAsset.DiffQuoteQuantity.ToString0(),
                basketAsset.DiffPercentage.ToString0(),

                (basketAsset.DiffPercentage * basketAsset.ValueUsdt / 100).ToString0(),
                GetUsdtValue(basketAsset.Symbol, basketAsset.Quantity)
            );

            b.AppendLine(t);
        }

        LastOverviewMessageCsv = b.ToString();


        b.Clear();
        t = string.Format("Symbol;Price;Quantity");
        b.AppendLine(t);
        foreach (BasketAsset basketAsset in Basket)
        {
            t = string.Format("{0};{1};{2};{3:N2}",
                basketAsset.Base,

                basketAsset.Price.ToString0(),
                basketAsset.Quantity.ToString0(),
                GetUsdtValue(basketAsset.Symbol, basketAsset.Quantity)
            );

            b.AppendLine(t);
        }

        LastOverviewMessageCsvShort = b.ToString();

    }


    /// <summary>
    /// Bereken de totale waarde van het mandje
    /// Deze waarde is gebaseerd op de laatste prijs (symbol.LastPrice)
    /// </summary>
    /// <param name="totalValue"></param>
    /// <returns></returns>
    public bool CalculateBasketValue()
    {
        TotalValue = 0;
        foreach (BasketAsset basketAsset in Basket)
        {
            if (basketAsset == BasketMain)
            {
                basketAsset.Price = 1;

                if (TradeAccount.AssetList.TryGetValue(useUsdQuote, out CryptoAsset asset))
                    basketAsset.Quantity = asset.Total;
                else
                    basketAsset.Quantity = 0.0m;
            }
            else
            {
                // De prijs en aantal opzoeken
                basketAsset.Price = (decimal)basketAsset.Symbol.LastPrice;

                if (TradeAccount.AssetList.TryGetValue(basketAsset.Symbol.Base, out CryptoAsset asset))
                    basketAsset.Quantity = asset.Total;
                else
                    basketAsset.Quantity = 0.0m;
            };

            basketAsset.QuoteQuantity = basketAsset.Quantity * basketAsset.Price;
            TotalValue += basketAsset.QuoteQuantity;

            // Quick en dirty wat USDT prijzen en waarden berekenen voor de display
             if ((Exchange.SymbolListName.TryGetValue(basketAsset.Base + useUsdQuote, out CryptoSymbol? symbol2)) && symbol2.LastPrice.HasValue)
                basketAsset.ValueUsdt = basketAsset.Quantity * (decimal)symbol2.LastPrice;
            else
                basketAsset.ValueUsdt = 0;

        };

        return true;
    }


    public static decimal GetUsdtValue(CryptoSymbol symbol, decimal quantity)
    {
        if (symbol == null)
            return quantity;


        if (symbol.Quote == "USDT")
        {
            if (symbol.LastPrice.HasValue)
                return (decimal)symbol.LastPrice * quantity;
            else return 0;
        }
        else if (symbol.Exchange.SymbolListName.TryGetValue(symbol.Base + useUsdQuote, out CryptoSymbol? SymbolXxx))
        {
            if (SymbolXxx.LastPrice.HasValue)
                return (decimal)SymbolXxx.LastPrice * quantity;
            else return 0;
        }
        else if (symbol.Exchange.SymbolListName.TryGetValue(useUsdQuote + symbol.Base, out SymbolXxx))
        {
            if (SymbolXxx.LastPrice.HasValue)
                return quantity / (decimal)SymbolXxx.LastPrice;
            else return 0;
        }
        else return 0;
    }


    private async Task<bool> BalanceAsset(CryptoDatabase databaseThread, CryptoTradeAccount tradeAccount, BasketAsset basketAsset, decimal price, decimal minimalBarometer, string side, int direction, StringBuilder newAdviceMessage)
    {
        // Barometer gebruiken we als vangnet als de BTC dropped (dat heeft niet altijd een positief effect)
        if (!ValidBarometer(basketAsset, minimalBarometer))
            return false;


        decimal diffQuantity = Math.Abs(basketAsset.DiffQuantity);

        price = price.Clamp(basketAsset.Symbol.PriceMinimum, basketAsset.Symbol.PriceMaximum, basketAsset.Symbol.PriceTickSize);
        diffQuantity = diffQuantity.Clamp(basketAsset.Symbol.QuantityMinimum, basketAsset.Symbol.QuantityMaximum, basketAsset.Symbol.QuantityTickSize);

        // Controleer de limiten van de berekende bedragen, Minimum bedrag
        if (!basketAsset.Symbol.InsideBoundaries(diffQuantity, price, out string textBoundaries))
        {
            // Hier wille we juist geen melding van (de balance bot doet enkel een poging tot handelen)
            //GlobalData.AddTextToLogTab(string.Format("{0} {1}", basketAsset.Symbol.Name, textBoundaries));
            return false;
        }



        if (GlobalData.Settings.BalanceBot.ShowAdviceOnly)
        {
            if (!Simulation)
            {
                string text = string.Format("Balancing advice {0} {1} price={2} amount={3} totaal={4} (${5:N2})",
                side, basketAsset.Symbol.Name, price.ToString0(),
                diffQuantity.ToString0(), (price * diffQuantity).ToString0(),
                GetUsdtValue(basketAsset.Symbol, Math.Abs(basketAsset.DiffQuantity)));

                newAdviceMessage.AppendLine(text);
            }
            return false;
        }

        // TODO: Rekening houden met fees, en iets extra vanwege tegenvallers met market buy/sell order 
        // (dat geeft verliezen), maar eerst standaard alles zonder fee berekening (vindt ik ingewikkeld).
        // Andere methode is een order die binnen zoveel seconden vervalt (waardoor je niet tientallen dezelfde orders plaatst)


        // Ik heb slecht ervaringen met limit orders (denk aan tientallen niet gevulde orders die blijven staan!)
        // Met een marketorder betaal e in principe wel ietsjes meer fee dacht ik (ooit nog eens nagaan of dat zo is)


        if (Simulation)
        {
            // bijwerken van de asset (direction is positief bij buy, negatief bij een sell )
            basketAsset.Quantity += direction * diffQuantity;
            basketAsset.QuoteQuantity = 0; // usdt display resetten

            // Dit gaat af van de BTC voorraad  (direction is positief):
            BasketMain.Quantity -= direction * diffQuantity * (decimal)basketAsset.Symbol.LastPrice;

            //MainQuantity = BasketMain.Quantity;
            TradeCount++;

            // Registreer het aantal gekochte en verkochte muntjes zodat we "performance" kunnen meten.
            // En registreer tevens de laatste waarde zodat we "performance" kunnen meten
            basketAsset.Symbol.QuantityTest += direction * diffQuantity; // Direction is bij een buy positief en bij een sell negatief
            basketAsset.Symbol.QuoteQuantityTest = basketAsset.Symbol.QuantityTest * (decimal)basketAsset.Symbol.LastPrice;
            BasketMain.Symbol.QuantityTest = BasketMain.Quantity;
            BasketMain.Symbol.QuoteQuantityTest = BasketMain.Quantity;


            // Trade simuleren
            CryptoTrade trade = new();
            trade.TradeAccount = tradeAccount;
            trade.TradeAccountId = tradeAccount.Id;
            if (side == "buy")
                trade.Side = CryptoOrderSide.Buy;
            else
                trade.Side = CryptoOrderSide.Sell;
            trade.Quantity = diffQuantity;
            trade.Price = (decimal)basketAsset.Symbol.LastPrice;
            trade.QuoteQuantity = trade.Quantity * trade.Price;
            trade.Id = basketAsset.Symbol.TradeList.Count + 1;
            basketAsset.Symbol.TradeList.Add(trade.Id, trade);



            // Asset bijwerken
            
            if (!tradeAccount.AssetList.TryGetValue(basketAsset.Symbol.Base, out CryptoAsset asset))
            {
                asset = new CryptoAsset();
                asset.Quote = basketAsset.Symbol.Base;
                asset.TradeAccountId = tradeAccount.Id;
                tradeAccount.AssetList.Add(asset.Quote, asset);
            }
            asset.Free = basketAsset.Quantity;
            asset.Total = basketAsset.Quantity;

            // Asset BTC bijwerken
            if (!tradeAccount.AssetList.TryGetValue(BasketMain.Symbol.Base, out asset))
            {
                asset = new CryptoAsset();
                asset.Quote = BasketMain.Symbol.Base;
                asset.TradeAccountId = tradeAccount.Id;
                tradeAccount.AssetList.Add(asset.Quote, asset);
            }
            asset.Free = BasketMain.Quantity;
            asset.Total = BasketMain.Quantity;


        }
        else
        {
            BinanceWeights.WaitForFairWeight(1);


            CryptoOrderType orderType = CryptoOrderType.Limit;
            if (GlobalData.Settings.BalanceBot.UseMarketOrder)
                orderType = CryptoOrderType.Market;

            (bool result, TradeParams tradeParams) result;
            BinanceApi exchangeApi = new(tradeAccount, basketAsset.Symbol, DateTime.UtcNow);
            if (side == "buy")
                result = await exchangeApi.BuyOrSell(orderType, CryptoOrderSide.Buy,  diffQuantity, price, null, null);
            else
                result = await exchangeApi.BuyOrSell(orderType, CryptoOrderSide.Sell, diffQuantity, price, null, null);

            if (result.result)
            {
                TradeCount++;
                string text = string.Format("Balancing {0} {1} price={2} amount={3} totaal={4} (${5:N2})",
                    side, basketAsset.Symbol.Name, result.tradeParams.Price.ToString0(),
                    result.tradeParams.Quantity.ToString0(), (result.tradeParams.Price * result.tradeParams.Quantity).ToString0(),
                    GetUsdtValue(basketAsset.Symbol, Math.Abs(result.tradeParams.Quantity)));
                GlobalData.AddTextToLogTab(text);
                GlobalData.AddTextToTelegram(text);

                CryptoBalance balance = new();
                balance.EventTime = CurrentDate;
                balance.Name = basketAsset.Symbol.Name;
                balance.Price = result.tradeParams.Price;
                balance.Quantity = -direction * result.tradeParams.Quantity; // direction is positief bij buy, negatief bij een sell
                balance.QuoteQuantity = balance.Price * balance.Quantity;
                balance.UsdtValue = basketAsset.ValueUsdt;

                // Voor een beter overzicht wat er in de coin zit (op dit moment)
                balance.InvestedQuantity = basketAsset.Quantity + direction * diffQuantity; // direction is positief bij buy, negatief bij een sell
                balance.InvestedValue = balance.InvestedQuantity * result.tradeParams.Price;
                databaseThread.Connection.Insert<CryptoBalance>(balance);
            }
        }

        return true;
    }



    //private bool RefreshExchangeInfo()
    //{
    //    if (Simulation)
    //        return true;

    //    BinanceWeights.WaitForFairBinanceWeight(5, $"{ExchangeOptions.ExchangeName} account info");
    //    using (var client = new BinanceClient())
    //    {
    //        // Eigenlijk hebben we die assets al (via de trades, maar ook via de balance items)
    //        WebCallResult<BinanceAccountInfo> accountInfo = client.General.GetAccountInfo();
    //        if (accountInfo.Success)
    //        {
    //            BinanceTools.PickupAssets(Exchange, accountInfo.Data.Balances);
    //            return true;
    //        }
    //    }
    //    return false;
    //}


    private static bool DetermineBasketMainValue(CryptoDatabase databaseThread)
    {
        // Besloten om dit uit te zetten (maar gaat het dan wel goed?
        return true;

        //if (Simulation)
        //{
        //    BasketMain.Quantity = MainQuantity;
        //    return true;
        //}

        //// Bepaal de totale inleg = totale som van inleg - balanceer investeringen
        //// NB: Het aantal balanceer bewegingen wordt na verloop van tijd "groot", hoe op te lossen?
        //string sql = string.Format("select sum(QuoteQuantity) as Total from balances");
        //decimal? result = databaseThread.Query<decimal?>(sql).SingleOrDefault();
        //decimal sumMainQuote = (result == null) ? 0m : (decimal)result;
        //if (sumMainQuote <= 0)
        //{
        //    string text = "Balancing: Geen inleg, afgebroken!";
        //    GlobalData.AddTextToLogTab(text);
        //    GlobalData.AddTextToTelegram(text);
        //    return false;
        //}
        //// De grote portefeille
        //BasketMain.Quantity = sumMainQuote;
        //return true;
    }


    /// <summary>
    /// De magic van het balanceren tussen de aangeboden muntjes
    /// Bij het bereiken van de opgegeven threshold gaan we balanceren.
    /// (in de praktijk is dat echter ook pas bij een bepaalde (minimum) amount)
    /// </summary>
    private async Task BalanceBasket(CryptoTradeAccount tradeAccount)
    {
        using CryptoDatabase databaseThread = new();
        {
            databaseThread.Close();
            databaseThread.Open();

            // De MainBasket staat op dit moment UIT (hardcoded USDT of ETH)
            if (DetermineBasketMainValue(databaseThread))
            {
                //if (RefreshExchangeInfo())
                {
                    // De waarde van het mandje bepalen (ten tijde van candle X)
                    // Als we van alle munten een waarde hebben kunnen we balanceren
                    if (CalculateBasketValue())
                    {
                        // Wat en hoeveel willen we balanceren? De verdeelsleutel is nu gelijk
                        foreach (BasketAsset basketAsset in Basket)
                        {
                            // Hier wordt niet op gehandeld, is de portefeuille
                            if (basketAsset == BasketMain)
                                continue;

                            // Dit is de waarde wat we in de munt willen hebben (momenteel evenredig verdeeld)
                            // (met dezelfde percentages is dit overal gelijk, een constante in de spreadsheet, B$18)
                            basketAsset.WantedValue = (basketAsset.Percentage / 100) * TotalValue; // / Basket.Count;
                                                                                                   // Dit is het aantal wat we in de munt willen hebben
                            basketAsset.WantedQuantity = basketAsset.WantedValue / basketAsset.Price;
                            // Dit is het verschil in aantal wat we moeten kopen of verkopen
                            basketAsset.DiffQuantity = basketAsset.WantedQuantity - basketAsset.Quantity;
                            // Dit is het percentage van de aan- of verkoop aantal
                            basketAsset.DiffPercentage = 100 * basketAsset.DiffQuantity / basketAsset.WantedQuantity;
                            // Waarde uitgedrukt in BTC of USDT etc
                            basketAsset.DiffQuoteQuantity = basketAsset.Price * basketAsset.DiffQuantity;
                        }


                        // De informatie cachen zodat het opgevraagd kan worden
                        DumpBasketInfo();


                        // Het idee is om de asset met het hoogste threshold verschil het eerste uit te voeren,
                        // en daarna in een volgende loop (dat duurt hooguit 1 minuut) de volgende asset te doen.
                        Basket.Sort((x, y) => Math.Abs(y.DiffPercentage).CompareTo(Math.Abs(x.DiffPercentage)));


                        StringBuilder newAdviceMessage = new();
                        foreach (BasketAsset basketAsset in Basket)
                        {
                            // Hier wordt niet op gehandeld, is de portefeuille
                            if (basketAsset == BasketMain)
                                continue;

                            if (basketAsset.DiffPercentage > GlobalData.Settings.BalanceBot.BuyThresholdPercentage) // kopen voor de bidPrice
                            {
                                await BalanceAsset(databaseThread, tradeAccount, basketAsset, (decimal)basketAsset.Symbol.BidPrice, GlobalData.Settings.BalanceBot.MinimalBuyBarometer, "buy", +1, newAdviceMessage);
                                //break;
                            }
                            else if (basketAsset.DiffPercentage < -GlobalData.Settings.BalanceBot.SellThresholdPercentage) // verkopen voor de askPrice
                            {
                                await BalanceAsset(databaseThread, tradeAccount, basketAsset, (decimal)basketAsset.Symbol.AskPrice, GlobalData.Settings.BalanceBot.MinimalSellBarometer, "sell", -1, newAdviceMessage);
                                //break;
                            }
                        }

                        // Probeer het aantal meldingen te reduceren (aangezien het praktisch real live is)
                        // Met name telegram vindt teveel berichten helemaal niet leuk (excepties)
                        string text = newAdviceMessage.ToString();
                        if ((text != "") && (LastAdviceMessage != text))
                        {
                            LastAdviceMessage = text;
                            GlobalData.AddTextToLogTab(text);
                            GlobalData.AddTextToTelegram(text);
                        }

                    }
                }
            }
        }
    }



    private BasketAsset AddSymbolToBasket(string symbolName, decimal percentage)
    {
        if (Exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            foreach (BasketAsset b in Basket)
            {
                if (b.Symbol.Name.Equals(symbolName))
                    throw new Exception(symbolName + " staat al in de balanceer lijst");
            }

            if (symbol.Quote != useUsdQuote)
                throw new Exception(symbolName + " heeft niet de gewenste quote");



            // De prijzen zijn nog niet gezet, exit
            if (!symbol.AskPrice.HasValue)
                return null;
            if (!symbol.BidPrice.HasValue)
                return null;
            if (!symbol.LastPrice.HasValue)
                return null;

            BasketAsset balanceAsset = new();
            balanceAsset.Symbol = symbol;
            balanceAsset.Base = symbol.Base;
            balanceAsset.Quote = symbol.Quote;
            balanceAsset.Quantity = 0m;
            balanceAsset.Percentage = percentage;
            Basket.Add(balanceAsset);

            return balanceAsset;
        }
        else throw new Exception(symbolName + " bestaat niet");
    }



    private async Task ExecuteInternal()
    {
        // Reset
        Basket.Clear();
        BasketMain = null;
        CurrentDate = DateTime.UtcNow;

        try
        {
            if (GlobalData.Settings.BalanceBot.CoinList.Any())
            {
                if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Exchange))
                    throw new Exception($"Exchange {ExchangeOptions.ExchangeName} niet aanwezig");

                // De te balanceren munten
                foreach (BalanceSymbol balanceSymbol in GlobalData.Settings.BalanceBot.CoinList)
                {
                    BasketAsset basketAsset = AddSymbolToBasket(balanceSymbol.Symbol, balanceSymbol.Percentage);
                    if (basketAsset == null)
                        return; // Task.CompletedTask;
                }

                if (Basket.Count == 0)
                    throw new Exception("Er zijn geen niet genoeg munten om te balanceren");


                // Het "wisselkantoor" voor deze munten (bah, hardcoded)
                // De vraag is of deze toevoeging wel nodig is? (via voorbeeld erin, maar who cares!)
                //BasketMain = AddSymbolToBasket("BTCUSDT", 0m);
                //if (BasketMain == null)
                //  return;

                BasketMain = new BasketAsset();
                BasketMain.Symbol = null;
                BasketMain.Base = useUsdQuote;
                BasketMain.Quote = useUsdQuote;
                BasketMain.Quantity = 0m;
                BasketMain.Percentage = 0;
                Basket.Add(BasketMain);


                // Het portfolio balanceren
                Basket.Sort((x, y) => x.Base.CompareTo(y.Base));
                await BalanceBasket(TradeAccount);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR balancing: " + error.ToString());
        }
    }



    public async Task Execute()
    {
        // Voorkom dubbele aanvragen (nergens voor nodig)
        if (Monitor.TryEnter(GlobalData.Settings.BalanceBot))
        {
            try
            {
                await ExecuteInternal();
            }
            finally
            {
                Monitor.Exit(GlobalData.Settings.BalanceBot);
            }
        }
    }


}
#endif
