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

    /// <summary>
    /// With small task bar buttons Windows shows the 16 pixel picture; the badge there takes a bit
    /// over three fifths of the icon (three quarters hid the coin), the larger sizes keep the
    /// smaller badge.
    /// </summary>
    [TestMethod]
    public void TheSmallSizes_GetABiggerBadge()
    {
        Assert.AreEqual(10f, AppIconBuilder.BadgeDiameter(16));
        Assert.AreEqual(12f, AppIconBuilder.BadgeDiameter(20));
        Assert.AreEqual(15f, AppIconBuilder.BadgeDiameter(24));
        Assert.AreEqual(18f, AppIconBuilder.BadgeDiameter(32));
        Assert.AreEqual(143f, AppIconBuilder.BadgeDiameter(256));
    }

    /// <summary>
    /// The artwork has a margin of a fifth of the picture, which is too much to spare in 16 pixels:
    /// up to 24 pixels it is drawn to the edges, from 32 pixels on it keeps its margin.
    /// </summary>
    [TestMethod]
    public void TheSmallSizes_FillThePicture_TheLargeOnesKeepTheirMargin()
    {
        byte[] ico = AppIconBuilder.Build(null, false);
        foreach (int size in new[] { 16, 20, 24 })
        {
            using SKBitmap bitmap = DecodeEntry(ico, size);
            (int width, int height) = ContentSize(bitmap);
            Assert.IsTrue(height >= size - 1, $"{size} pixels: the artwork is {height} pixels high");
            Assert.IsTrue(width >= size - 2, $"{size} pixels: the artwork is {width} pixels wide");
        }

        foreach (int size in new[] { 32, 48, 256 })
        {
            using SKBitmap bitmap = DecodeEntry(ico, size);
            (_, int height) = ContentSize(bitmap);
            Assert.IsTrue(height < size * 0.9, $"{size} pixels: the artwork is {height} pixels high");
        }
    }

    /// <summary>
    /// GTK (Photino on Linux) cannot read a PNG inside an .ico and then shows no icon at all, so only
    /// the 256 pixel picture is one. The bitmaps still have to come out the right way up: the 32
    /// pixel bitmap has to look like the 256 pixel PNG made smaller, not like it upside down.
    /// </summary>
    [TestMethod]
    public void TheSmallerPictures_AreBitmaps_TheRightWayUp()
    {
        byte[] ico = AppIconBuilder.Build(CryptoExchangeType.Binance, false);
        int count = BitConverter.ToUInt16(ico, 4);
        for (int i = 0; i < count; i++)
        {
            int entry = 6 + 16 * i;
            int size = ico[entry] == 0 ? 256 : ico[entry];
            int offset = BitConverter.ToInt32(ico, entry + 12);
            bool isPng = ico[offset] == 0x89 && ico[offset + 1] == (byte)'P';
            Assert.AreEqual(size == 256, isPng, $"{size} pixels");
        }

        using SKBitmap bitmap = DecodeEntry(ico, 32);
        using SKBitmap large = DecodeEntry(ico, 256);
        using SKBitmap expected = large.Resize(new SKImageInfo(32, 32), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        double straight = Difference(bitmap, expected, false);
        double upsideDown = Difference(bitmap, expected, true);
        Assert.IsTrue(straight < upsideDown / 2, $"difference {straight:0.0} the right way up, {upsideDown:0.0} upside down");
    }

    /// <summary>
    /// The Dock on macOS gets the 256 pixel picture as a PNG. Asking for it anywhere else does
    /// nothing: the macOS service returns at once, and on Windows and Linux the window icon is
    /// all there is.
    /// </summary>
    [TestMethod]
    public void TheDockPicture_IsThe256PixelPng_AndOnlyMacOSUsesIt()
    {
        byte[] png = AppIconBuilder.BuildPng(CryptoExchangeType.Binance, true);
        using SKBitmap? bitmap = SKBitmap.Decode(png);
        Assert.IsNotNull(bitmap);
        Assert.AreEqual(256, bitmap.Width);
        Assert.AreEqual(256, bitmap.Height);

        ((CryptoScanner.Core.Services.IPlatformService)new CryptoScanner.Core.Services.MacOSPlatformService()).ApplyApplicationIcon(png);
        ((CryptoScanner.Core.Services.IPlatformService)new CryptoScanner.Core.Services.WindowsPlatformService()).ApplyApplicationIcon(png);
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

            if (size < 256)
            {
                // A bitmap: its header, the picture and the mask, at twice the height
                Assert.AreEqual(40, BitConverter.ToInt32(ico, offset), $"Entry {i} header");
                Assert.AreEqual(size, BitConverter.ToInt32(ico, offset + 4), $"Entry {i} width");
                Assert.AreEqual(size * 2, BitConverter.ToInt32(ico, offset + 8), $"Entry {i} height");
                Assert.AreEqual(32, BitConverter.ToUInt16(ico, offset + 14), $"Entry {i} bits per pixel");
                Assert.AreEqual(40 + size * (size * 4 + (size + 31) / 32 * 4), length, $"Entry {i} length");
            }

            using SKBitmap bitmap = DecodeEntry(ico, size);
            Assert.AreEqual(size, bitmap.Width, $"Entry {i} width");
            Assert.AreEqual(size, bitmap.Height, $"Entry {i} height");
            sizes.Add(size);
        }
        return sizes;
    }

    /// <summary>
    /// One picture of the .ico, read by Skia's own .ico decoder - the one Avalonia uses on Linux - by
    /// handing it an .ico that holds just that picture.
    /// </summary>
    private static SKBitmap DecodeEntry(byte[] ico, int wanted)
    {
        int count = BitConverter.ToUInt16(ico, 4);
        for (int i = 0; i < count; i++)
        {
            int entry = 6 + 16 * i;
            int size = ico[entry] == 0 ? 256 : ico[entry];
            if (size != wanted)
                continue;

            int length = BitConverter.ToInt32(ico, entry + 8);
            int offset = BitConverter.ToInt32(ico, entry + 12);
            byte[] single = new byte[6 + 16 + length];
            ico.AsSpan(0, 4).CopyTo(single);
            BitConverter.TryWriteBytes(single.AsSpan(4), (ushort)1);
            ico.AsSpan(entry, 12).CopyTo(single.AsSpan(6));
            BitConverter.TryWriteBytes(single.AsSpan(6 + 12), 6 + 16);
            ico.AsSpan(offset, length).CopyTo(single.AsSpan(6 + 16));
            return SKBitmap.Decode(single) ?? throw new AssertFailedException($"Skia cannot read the {wanted} pixel picture");
        }
        throw new AssertFailedException($"No {wanted} pixel picture");
    }

    /// <summary>The width and the height of the part of a picture that is not (almost) transparent.</summary>
    private static (int Width, int Height) ContentSize(SKBitmap bitmap)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha <= 20)
                    continue;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }
        return (right - left + 1, bottom - top + 1);
    }

    /// <summary>Mean difference per pixel over the four channels, optionally against <paramref name="b"/> upside down.</summary>
    private static double Difference(SKBitmap a, SKBitmap b, bool flip)
    {
        double total = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor p = a.GetPixel(x, y);
                SKColor q = b.GetPixel(x, flip ? b.Height - 1 - y : y);
                total += Math.Abs(p.Red - q.Red) + Math.Abs(p.Green - q.Green) + Math.Abs(p.Blue - q.Blue) + Math.Abs(p.Alpha - q.Alpha);
            }
        }
        return total / (a.Width * a.Height);
    }
}
