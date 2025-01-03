﻿using CryptoExchange.Net.Authentication;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class Api : ExchangeBase
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute]
    public Api()
    {
        Asset = new Asset();
        Candle = new Candle(this);
        Symbol = new Symbol();
        Order = new Order();
        Trade = new Trade();
    }

    public override IDisposable GetClient()
    {
        return new MexcRestClient();
    }

    public override void ExchangeDefaults()
    {
        ExchangeOptions.CandleLimit = 500;
        ExchangeOptions.ExchangeName = "Mexc Spot";
        ExchangeOptions.LimitAmountOfSymbols = true;
        ExchangeOptions.SymbolLimitPerSubscription = 3; // 10 was okay for limited amount, but errors with 2500+ symbols, back to 1
        ExchangeOptions.SubscriptionLimitPerClient = 10;

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} defaults");

        // Default opties voor deze exchange
        MexcRestClient.SetDefaultOptions(options =>
        {
            //options.OutputOriginalData = true;
            //options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            options.RequestTimeout = TimeSpan.FromSeconds(40); // standard=20 seconds
            //options.SpotOptions.RateLimiters = ?
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        MexcSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.RequestTimeout = TimeSpan.FromSeconds(60); // standard=20 seconds
            options.ReconnectInterval = TimeSpan.FromSeconds(10); // standard=5 seconds
            options.SocketNoDataTimeout = TimeSpan.FromMinutes(1); // standard=30 seconds

            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        PriceTicker = new Ticker(ExchangeOptions, typeof(SubscriptionPriceTicker), CryptoTickerType.price);
        KLineTicker = new Ticker(ExchangeOptions, typeof(SubscriptionKLineTicker), CryptoTickerType.kline);
        // UserTicker = new Ticker(ExchangeOptions, typeof(SubscriptionUserTicker), CryptoTickerType.user);
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.LimitMaker => CryptoOrderType.StopLimit, /// ????????????????????????????????????????????????
            _ => throw new Exception("Niet ondersteunde ordertype"),
        };

        return localOrderType;
    }

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Niet ondersteunde orderside"),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(Bybit.Net.Enums.V5.OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            Bybit.Net.Enums.V5.OrderStatus.New => CryptoOrderStatus.New,
            Bybit.Net.Enums.V5.OrderStatus.Filled => CryptoOrderStatus.Filled,
            Bybit.Net.Enums.V5.OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            Bybit.Net.Enums.V5.OrderStatus.PartiallyFilledCanceled => CryptoOrderStatus.PartiallyAndClosed, // niet alles kon omgezet worden, iets minder gekregen
            //Bybit.Net.Enums.V5.OrderStatus.Expired => CryptoOrderStatus.Expired,
            Bybit.Net.Enums.V5.OrderStatus.Cancelled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public override Task<(bool result, TradeParams? tradeParams)> PlaceOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, DateTime currentDate,
        CryptoOrderType orderType, CryptoOrderSide orderSide,
        decimal quantity, decimal price, decimal? stop, decimal? limit, bool generateJsonDebug = false)
    {
        // not implemented
        return Task.FromResult<(bool succes, TradeParams? tradeParams)>((false, null));
    }


    public override Task<(bool succes, TradeParams? tradeParams)> Cancel(CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
    {
        // not implemented
        return Task.FromResult<(bool succes, TradeParams? tradeParams)>((false, null));
    }

}