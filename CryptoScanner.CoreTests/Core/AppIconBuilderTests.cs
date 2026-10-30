using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using SkiaSharp;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The window icon with the logo of the exchange on it (AppIconBuilder), which tells several
/// scanners side by side apart in the task bar.
/// </summary>
[TestClass]
public class AppIconBuilderTests
{
    private static readonly int[] ExpectedSizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

    /// <summary>
    /// A new exchange without a logo would quietly get the plain icon; this makes it show up here
    /// instead.
    /// </summary>
    [TestMethod]
    public void EveryExchange_HasALogo()
    {
        var assembly = typeof(AppIconBuilder).Assembly;
        foreach (CryptoExchangeType exchangeType in Enum.GetValues<CryptoExchangeType>())
        {
            string name = $"CryptoScanner.Core.Assets.Exchanges.{AppIconBuilder.LogoName(exchangeType)}.png";
            using Stream? stream = assembly.GetManifestResourceStream(name);
            Assert.IsNotNull(stream, $"No logo for {exchangeType} ({name})");
        }
    }

    /// <summary>
    /// Windows picks the picture closest to the size it needs, so every size has to be there, and
    /// each has to be a picture of the size the directory says it is.
    /// </summary>
    [TestMethod]
    public void TheIcon_HoldsEverySize()
    {
        CryptoExchangeType?[] exchanges = [null, CryptoExchangeType.Binance, CryptoExchangeType.HyperLiquid];
        foreach (bool emulator in new[] { false, true })
        {
            foreach (CryptoExchangeType? exchangeType in exchanges)
            {
                byte[] ico = AppIconBuilder.Build(exchangeType, emulator);
                List<int> sizes = ReadSizes(ico);
                CollectionAssert.AreEqual(ExpectedSizes, sizes, $"{exchangeType} emulator={emulator}");
            }
        }
    }

    [TestMethod]
    public void TheBadgeAndTheEmulator_EachChangeTheIcon()
    {
        byte[] plain = AppIconBuilder.Build(null, false);
        byte[] binance = AppIconBuilder.Build(CryptoExchangeType.Binance, false);
        byte[] emulatorBinance = AppIconBuilder.Build(CryptoExchangeType.Binance, true);

        Assert.IsFalse(plain.SequenceEqual(binance), "The badge does not show");
        Assert.IsFalse(binance.SequenceEqual(emulatorBinance), "The emulator looks like the scanner");
    }

    /// <summary>Bybit EU is the same brand, so it carries the same logo.</summary>
    [TestMethod]
    public void BybitEu_SharesTheBybitLogo()
    {
        Assert.IsTrue(AppIconBuilder.Build(CryptoExchangeType.Bybit, false)
            .SequenceEqual(AppIconBuilder.Build(CryptoExchangeType.BybitEu, false)));
    }

    /// <summary>Decode every picture in the .ico and check it against its directory entry.</summary>
    private static List<int> ReadSizes(byte[] ico)
    {
        Assert.AreEqual(0, BitConverter.ToUInt16(ico, 0), "reserved");
        Assert.AreEqual(1, BitConverter.ToUInt16(ico, 2), "type icon");

        List<int> sizes = [];
        int count = BitConverter.ToUInt16(ico, 4);
        for (int i = 0; i < count; i++)
        {
            int entry = 6 + 16 * i;
            int size = ico[entry] == 0 ? 256 : ico[entry];
            int length = BitConverter.ToInt32(ico, entry + 8);
            int offset = BitConverter.ToInt32(ico, entry + 12);

            using SKBitmap? bitmap = SKBitmap.Decode(ico.AsSpan(offset, length).ToArray());
            Assert.IsNotNull(bitmap, $"Entry {i} is not a picture");
            Assert.AreEqual(size, bitmap.Width, $"Entry {i} width");
            Assert.AreEqual(size, bitmap.Height, $"Entry {i} height");
            sizes.Add(size);
        }
        return sizes;
    }
}
