using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using Kucoin.Net.Clients;

namespace CryptoScanBot.Exchange.Kucoin;
#if TRADEBOT
public class TickerUserItem(ExchangeOptions exchangeOptions) : TickerUserBase(exchangeOptions)
{
    private readonly KucoinSocketClient socketClient;
    private readonly UpdateSubscription _subscription;

    public override async Task StopAsync()
    {
        if (_subscription == null)
            return;

        GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} Stopping user ticker");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient.SpotApi.UnsubscribeAllAsync();
        return;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async Task StartAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // todo
        //using KucoinRestClient client = new();
        {
            //CallResult<string> userStreamResult = await client.V5Api.Account.StartUserStreamAsync();
            //if (!userStreamResult.Success)
            //{
            //    GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} Error starting user stream: " + userStreamResult.Error.Message);
            //    return;
            //}


            //socketClient = new();
            //var subscriptionResult = await socketClient.V5SpotStreams.SubscribeToTradeUpdatesAsync(
            //    userStreamResult.Data,
            //    OnOrderUpdate,
            //    null,
            //    OnAccountPositionMessage,
            //    null
            //    ).ConfigureAwait(false);

            //// Subscribe to network-related stuff
            //if (userStreamResult.Success)
            //{
            //    _subscription = subscriptionResult.Data;

            //    // Events
            //    _subscription.Exception += Exception;
            //    _subscription.ConnectionLost += ConnectionLost;
            //    _subscription.ConnectionRestored += ConnectionRestored;
            //    return;
            //}
            //else
            //{
            //    GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} Error subscribing to spot.userstream " + subscriptionResult.Error.Message);
            //    return;
            //}
        }

    }

}

#endif
