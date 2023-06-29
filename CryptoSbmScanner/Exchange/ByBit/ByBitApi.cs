using Bybit.Net.Enums;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.ByBit;


public class ByBitApi
{

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    // Default opties voor deze exchange
    public static void SetExchangeDefaults()
    {
        //BinanceClient.SetDefaultOptions(new BinanceClientOptions() { });
        //BinanceSocketClientOptions options = new();
        //options.SpotStreamsOptions.AutoReconnect = true;
        //options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
        //BinanceSocketClient.SetDefaultOptions(options);
    }
}
