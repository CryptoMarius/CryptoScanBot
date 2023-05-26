using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.Trading;

public class BinanceApi
{

    public static async Task<(bool result, string reaction)> DoOnSignal(CryptoSignal signal)
    {

        //using (SqlConnection databaseThread = new SqlConnection(GlobalData.ConnectionString))
        {
            //databaseThread.Close();
            //databaseThread.Open();
            try
            {
                if (signal.Mode == TradeDirection.Long)
                {
                    string reaction = "";


                    // Is er een API key aanwezig (met de juiste opties)
                    if (!SymbolTools.CheckValidApikey(out reaction))
                        return (false, reaction);

                    // Hoeveel muntjes hebben we?
                    var resultPortFolio = SymbolTools.CheckPortFolio(signal.Symbol);
                    if (!resultPortFolio.result)
                        return (false, reaction);
                    decimal assetQuantity = resultPortFolio.value;

                    // Is er "geld" om de order te kunnen plaatsen?
                    // De Quantity is in Quote bedrag (bij BTCUSDT de USDT dollar)
                    if (!SymbolTools.CheckValidAmount(signal.Symbol, assetQuantity, out decimal buyAmount, out reaction))
                        return (false, reaction);

                    //Is de status van de munt nog steeds "running" (probleem i.v.m.) vertraging nieuws! <handmatig?>)
                    if (!await SymbolTools.CheckDelistedCoin(signal.Symbol))
                        return (false, "");


                    // Wat wordt de prijs? (hoe graag willen we in de trade?)
                    // Onderstaand moet door het (basis)algoritme geregeld worden en niet hier
                    decimal price = signal.Price;
                    switch (GlobalData.Settings.Trading.BuyMethod)
                    {
                        case BuyPriceMethod.BidPrice:
                            if (signal.Symbol.BidPrice.HasValue)
                                price = (decimal)signal.Symbol.BidPrice;
                            break;
                        case BuyPriceMethod.AskPrice:
                            if (signal.Symbol.AskPrice.HasValue)
                                price = (decimal)signal.Symbol.AskPrice;
                            break;
                        case BuyPriceMethod.BidAndAskPriceAvg:
                            if (signal.Symbol.AskPrice.HasValue)
                                price = (decimal)(signal.Symbol.AskPrice + signal.Symbol.BidPrice) / 2;
                            break;
                        // TODO: maar voorlopig even afgesterd
                        //case BuyPriceMethod.Sma20: 
                        //    if (price > (decimal)CandleData.Sma20)
                        //        price = (decimal)CandleData.Sma20;
                        //    break;
                        // TODO: maar voorlopig even afgesterd
                        //case BuyPriceMethod.LowerBollingerband:
                        //    decimal lowerBand = (decimal)(CandleData.Sma20 - CandleData.BollingerBandsDeviation);
                        //    if (price > lowerBand)
                        //        price = lowerBand;
                        //    break;
                        case BuyPriceMethod.MarketOrder:
                            price = (decimal)signal.Symbol.LastPrice;
                            break;
                    }


                    
                    // Plaats buy order
                    // Dit triggert een notificatie die technisch gezien eerder kan arriveren dan dat wij 
                    // de positie toevoegen, daarom locken we hier de posities voor het plaatsen van de buy.
                    Monitor.Enter(signal.Exchange.PositionList);
                    try
                    {
                        (bool result, TradeParams tradeParams) result = await PositionMonitor.PlaceBuyOrder(null, 0, signal.Symbol, price, "BUY", GlobalData.Settings.Trading.BuyMethod == BuyPriceMethod.MarketOrder);
                        if (result.result)
                        {
                            //Een positie maken
                            CryptoPosition position = new CryptoPosition();
                            //if (Signal.Id > 0)
                            //    position.SignalId = Signal.Id;
                            position.CreateTime = result.tradeParams.CreateTime;
                            position.Signal = signal;
                            position.SignalId = signal.Id;
                            position.Exchange = signal.Exchange;
                            position.ExchangeId = signal.Exchange.Id;
                            position.Symbol = signal.Symbol;
                            position.SymbolId = signal.Symbol.Id;
                            position.Interval = signal.Interval;
                            position.IntervalId = signal.Interval.Id;
                            position.Status = CryptoPositionStatus.positionWaiting;
                            position.BuyPrice = result.tradeParams.Price;
                            position.BuyAmount = result.tradeParams.QuoteQuantity; // voor het bepalen van het volgende aankoop bedrag (die in de settings kan wijzigen)
                            if (position.BuyAmount == 0)
                                position.BuyAmount = result.tradeParams.Price * result.tradeParams.Quantity;
                            //databaseThread.Insert<CryptoPosition>(position);

                            GlobalData.AddTextToLogTab(string.Format("Debug: positie status naar {0}", position.Status.ToString()));
                            GlobalData.AddPosition(position);
                            GlobalData.AddTextToLogTab(string.Format("Debug: positie toegevoegd {0}", position.Status.ToString()));

                            // Als vervanger van bovenstaande tzt
//TODO?
                            //CreatePositionStep(databaseThread, position, result.tradeParams, "BUY");
                        }
                    }
                    finally
                    {
                        Monitor.Exit(signal.Exchange.PositionList);
                    }
                    return (true, "");
                }

            }
            finally
            {
                //databaseThread.Close();
            }
        }

        return (false, "");
    }



}
