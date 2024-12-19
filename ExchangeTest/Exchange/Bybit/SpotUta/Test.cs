using Bybit.Net.Clients;

using CryptoExchange.Net.Authentication;

using CryptoScanBot.Core.Core;

namespace ExchangeTest.Exchange.Bybit.SpotUta;
internal class Test
{

    public static async void ByBitUtaSpotTestAsync()
    {
        GlobalData.LoadSettingsAsync();

        BybitRestClient.SetDefaultOptions(options =>
        {
            options.OutputOriginalData = true;
            options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BybitSocketClient.SetDefaultOptions(options =>
        {
            //options.AutoReconnect = true;
            options.OutputOriginalData = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });


        //string symbolName = "SUSHIUSDTM";
        //await CryptoScanBot.Kucoin.Futures.Symbols.GetSymbolsAsync();
        //await ExchangeTest.Exchange.Bybit.SpotUta.Candles.GetSymbolsAsync(symbolName);
    }
}
