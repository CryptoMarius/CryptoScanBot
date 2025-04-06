using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Coinbase.Spot;

public class Asset() : AssetBase(), IAsset
{

    Task IAsset.GetAssets(Model.CryptoExchange activeExchange)
    {
        throw new NotImplementedException();
    }
}
