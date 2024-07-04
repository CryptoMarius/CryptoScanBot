﻿using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using CryptoScanBot.Core.Exchange;

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
                        Symbol = symbol,
                        Exchange = exchange,
                        Side = CryptoTradeSide.Long,
                        Account = tradeAccount
                    };

                    CryptoPositionPart part = new();

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

