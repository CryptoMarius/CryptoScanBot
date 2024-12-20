using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;

namespace ExchangeTest.Exchange.Bybit.Spot;

internal class Test
{
    public static async Task BybitTestAsync()
    {
        CryptoDatabase? database = null;


        if (GlobalData.ExchangeListName.TryGetValue("Bybit Spot", out CryptoScanBot.Core.Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue("BTCEUR", out CryptoSymbol? symbol))
            {
                //CryptoAccount? tradeAccount = GlobalData.TradeAccountList.Values.FirstOrDefault(x => x.Name.Equals("Bybit Spot trading"));
                CryptoAccount? tradeAccount = null; // todo
                if (tradeAccount != null)
                {
                    CryptoPosition position = new()
                    {
                        Account = tradeAccount,
                        Exchange = exchange,
                        Symbol = symbol,
                        Interval = GlobalData.IntervalList[3],
                        Side = CryptoTradeSide.Long,
                    };

                    CryptoPositionPart part = new()
                    {
                        Position = position,
                        Exchange = exchange,
                        Symbol = symbol,
                    };

                    CryptoScanBot.Core.Exchange.BybitApi.Spot.Api api = new();
                    api.ExchangeDefaults();

                    decimal quantity = 0.00041m;
                    DateTime dateTime = DateTime.Now;
                    (bool result, TradeParams? tradeParams) result;

                    //result = await api.PlaceOrder(database, position, part, dateTime, CryptoOrderType.Limit, CryptoOrderSide.Buy, quantity, 50000m, null, null, true);
                    //ExchangeBase.Dump(position, result.result, result.tradeParams, "limit buy");

                    //result = await api.PlaceOrder(database, position, part, dateTime, CryptoOrderType.Limit, CryptoOrderSide.Sell, quantity, 75000m, null, null, true);
                    //ExchangeBase.Dump(position, result.result, result.tradeParams, "limit sell");

                    result = await api.PlaceOrder(database, position, part, dateTime, CryptoOrderType.StopLimit, CryptoOrderSide.Buy, quantity, 32000m, 31500m, null, true);
                    ExchangeBase.Dump(position, result.result, result.tradeParams, "stop limit buy");

                    //result = await api.PlaceOrder(database, position, part, dateTime, CryptoOrderType.StopLimit, CryptoOrderSide.Sell, quantity, 28000m, 28500m, null, true);
                    //ExchangeBase.Dump(position, result.result, result.tradeParams, "stop limit sell");

                    //result = await api.PlaceOrder(database, position, part, dateTime, CryptoOrderType.Oco, CryptoOrderSide.Buy, quantity, 28500, 32000m, 32500m, true);
                    //ExchangeBase.Dump(position, result.result, result.tradeParams, "OCO buy");

                    //result = await api.PlaceOrder(database, position, part, dateTime, CryptoOrderType.Oco, CryptoOrderSide.Sell, quantity, 32500, 28500m, 28000m, true);
                    //ExchangeBase.Dump(position, result.result, result.tradeParams, "OCO sell");
                }
            }
        }
    }
}

