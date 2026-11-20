using CryptoExchange.Net.Web3;
using HyperLiquid.Net;

namespace CryptoScanner.Core.Exchange.HyperLiquid;

/// <summary>
/// Builds the HyperLiquid credentials from the API key and secret of the settings. Shared by the Spot
/// and the Perpetual market.
///
/// <para>
/// HyperLiquid.Net 5.10.0 dropped the constructor that took the two strings (public address and private
/// key) and now wants <see cref="EthKey"/> instances. The key from the settings is the public address of
/// the account, the secret the private key that signs. The address goes in as the wallet and the
/// private key as the signer, so both an account key and an approved API wallet (agent) key keep
/// working: requests are signed with the private key, while user data is looked up on the address.
/// </para>
/// </summary>
internal static class HyperLiquidApiCredentials
{
    public static HyperLiquidCredentials Create(string key, string secret)
    {
        EthKey wallet = EthKey.FromPublicAddress(key);
        EthKey? signer = string.IsNullOrEmpty(secret) ? null : EthKey.FromPrivateKey(secret);
        return new HyperLiquidCredentials(new HyperLiquidCredential(wallet, signer!));
    }
}
