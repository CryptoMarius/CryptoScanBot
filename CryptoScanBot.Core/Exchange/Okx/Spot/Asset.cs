using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Okx.Spot;

public class Asset() : AssetBase(), IAsset
{

    Task IAsset.GetAssets(CryptoAccount tradeAccount)
    {
        throw new NotImplementedException();
    }
}
