using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Services;

using SkiaSharp;

using System.Collections.Concurrent;
using System.Reflection;

namespace CryptoScanner.Core.Core;

/// <summary>
/// Builds the window icon: the application icon with the logo of the active exchange as a round
/// badge in the bottom left corner (bottom right is where the magnifying glass sits). Several
/// scanners side by side all looked the same in the task bar; the badge tells them apart without
/// reading the title. The emulator starts from its own (purple) icon, so it is never mistaken for a
/// live scanner.
/// <para>
/// The result is a complete .ico with every size Windows asks for, because the 16 pixel icon in the
/// title bar and the 32 or 48 pixel one in the task bar each need their own badge and ring: scaling
/// one large picture down loses the ring and turns the logo into a smudge.
/// </para>
/// </summary>
public static class AppIconBuilder
{
    private const string ResourcePrefix = "CryptoScanner.Core.Assets.";

    /// <summary>The sizes Windows picks from (title bar, task bar, Alt-Tab, explorer, high DPI).</summary>
    private static readonly int[] IconSizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

    /// <summary>Badge diameter as a fraction of the icon.</summary>
    private const float BadgeFraction = 0.56f;

    /// <summary>
    /// Badge diameter as a fraction of the icon in the sizes up to <see cref="SmallIconSize"/>. With
    /// small task bar buttons Windows shows the 16 pixel picture (24 on the normal buttons, 20 and
    /// 24 at 125% and 150%), where the normal badge left a logo of 7 pixels. A bigger one (75%) took
    /// the coin away, so it is a compromise: the artwork is drawn to the edges there as well (see
    /// <see cref="Render"/>) and the badge takes a little over half of it.
    /// </summary>
    private const float SmallBadgeFraction = 0.62f;
    private const int SmallIconSize = 24;

    /// <summary>Pixels with less alpha than this count as empty when measuring the artwork.</summary>
    private const byte EmptyAlpha = 20;

    /// <summary>
    /// Only this size is stored as a PNG, like AppIcon.ico itself does; every smaller one is a bitmap.
    /// GTK, which Photino uses on Linux, cannot read a PNG inside an .ico ("Compressed icons are not
    /// supported") and then leaves the window without any icon at all.
    /// </summary>
    private const int PngSize = 256;

    /// <summary>
    /// Every settings save passes through ApplyConfigurationAsync and asks for the icon again; the
    /// exchange rarely changes, so the drawing is done once per combination.
    /// </summary>
    private static readonly ConcurrentDictionary<(CryptoExchangeType?, bool), byte[]> Cache = new();

    /// <summary>
    /// The icon for the running application and its exchange, or the plain application icon while
    /// no exchange has been chosen yet.
    /// </summary>
    public static byte[] BuildCurrent()
        => Build(CurrentExchangeType(), GlobalData.IsEmulatorMode);

    /// <summary>
    /// The exchange the icon should show. The window is created before ApplyConfigurationAsync has
    /// set the active exchange (Photino even has to have its icon before that), so fall back to the
    /// exchange in the settings. A new installation that still has to pick one shows no badge,
    /// rather than the default exchange it has not chosen.
    /// </summary>
    private static CryptoExchangeType? CurrentExchangeType()
    {
        if (GlobalData.ActiveExchange != null)
            return GlobalData.ActiveExchange.ExchangeType;
        if (GlobalData.ExchangeChoicePending)
            return null;
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? exchange))
            return exchange.ExchangeType;
        return null;
    }

    /// <summary>
    /// The icon as .ico bytes. Without an exchange (or for one without a logo) this is just the base
    /// icon, in the same sizes.
    /// </summary>
    public static byte[] Build(CryptoExchangeType? exchangeType, bool emulator)
        => Cache.GetOrAdd((exchangeType, emulator), key => Draw(key.Item1, key.Item2));

    /// <summary>
    /// The 256 pixel picture of the icon as a PNG, for a platform that takes a picture instead of an
    /// .ico (the Dock on macOS).
    /// </summary>
    public static byte[] BuildPng(CryptoExchangeType? exchangeType, bool emulator)
        => LargestPng(Build(exchangeType, emulator))
            ?? throw new InvalidOperationException("The icon holds no PNG picture");

    /// <summary>
    /// Hand the icon of the running application to the platform as the icon of the application as a
    /// whole. Only macOS needs it: its Dock never shows the icon of a window. Called next to every
    /// place that sets the window icon.
    /// </summary>
    public static void ApplyApplicationIcon()
    {
        try
        {
            GlobalData.GetService<IPlatformService>()?.ApplyApplicationIcon(BuildPng(CurrentExchangeType(), GlobalData.IsEmulatorMode));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AppIconBuilder.ApplyApplicationIcon");
        }
    }

    private static byte[] Draw(CryptoExchangeType? exchangeType, bool emulator)
    {
        using SKImage baseImage = LoadBaseImage(emulator);
        using SKImage? logo = exchangeType.HasValue ? LoadLogo(exchangeType.Value) : null;

        SKRect artwork = ArtworkBounds(baseImage);

        List<(int Size, byte[] Data)> entries = [];
        foreach (int size in IconSizes)
        {
            using SKImage picture = Render(baseImage, artwork, logo, size);
            entries.Add((size, size >= PngSize ? EncodePng(picture) : EncodeBitmap(picture)));
        }
        return WriteIco(entries);
    }

    /// <summary>
    /// Write the icon for the running application into <paramref name="folder"/> and return the path,
    /// for hosts that can only take an icon by file name (Photino). The name carries the exchange and
    /// the kind of application, so instances sharing a folder never overwrite each other's icon, and
    /// Windows - which caches icons loaded from a file by name - never shows a stale one after a
    /// switch. Empty when the file cannot be written; the caller then keeps the icon it has.
    /// </summary>
    public static string WriteCurrent(string folder)
    {
        try
        {
            CryptoExchangeType? exchangeType = CurrentExchangeType();
            string name = $"{(GlobalData.IsEmulatorMode ? "EmulatorIcon" : "AppIcon")}-{exchangeType?.ToString() ?? "None"}.ico";
            string path = Path.Combine(folder, name);
            byte[] icon = Build(exchangeType, GlobalData.IsEmulatorMode);

            // Another instance on the same exchange may be writing the very same bytes right now; if
            // so, the file it leaves behind is just as good as ours.
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(path, icon);
            }
            catch (IOException)
            {
                if (!File.Exists(path))
                    throw;
            }
            return path;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "AppIconBuilder.WriteCurrent");
            return "";
        }
    }

    /// <summary>
    /// The logo file of an exchange. The EU branch of Bybit is the same brand, so it shares the logo.
    /// </summary>
    internal static string LogoName(CryptoExchangeType exchangeType)
        => exchangeType switch
        {
            CryptoExchangeType.BybitEu => nameof(CryptoExchangeType.Bybit),
            _ => exchangeType.ToString(),
        };

    private static SKImage LoadBaseImage(bool emulator)
    {
        byte[] ico = ReadResource(emulator ? "EmulatorIcon.ico" : "AppIcon.ico")
            ?? throw new InvalidOperationException("The application icon is missing from the resources");

        // An .ico is a small directory of images. The 256 pixel one is stored as a PNG, which Skia
        // decodes directly; every smaller size is drawn from it.
        byte[]? bestPng = LargestPng(ico);

        // No PNG inside: let Skia's own .ico decoder pick an image.
        return SKImage.FromEncodedData(bestPng ?? ico)
            ?? throw new InvalidOperationException("The application icon cannot be decoded");
    }

    /// <summary>The largest picture inside an .ico that is stored as a PNG, or null when there is none.</summary>
    private static byte[]? LargestPng(byte[] ico)
    {
        int count = BitConverter.ToUInt16(ico, 4);
        int bestSize = 0;
        byte[]? bestPng = null;
        for (int i = 0; i < count; i++)
        {
            int entry = 6 + 16 * i;
            int size = ico[entry] == 0 ? 256 : ico[entry];
            int length = BitConverter.ToInt32(ico, entry + 8);
            int offset = BitConverter.ToInt32(ico, entry + 12);
            bool isPng = ico[offset] == 0x89 && ico[offset + 1] == (byte)'P' && ico[offset + 2] == (byte)'N' && ico[offset + 3] == (byte)'G';
            if (isPng && size > bestSize)
            {
                bestSize = size;
                bestPng = ico.AsSpan(offset, length).ToArray();
            }
        }
        return bestPng;
    }

    private static SKImage? LoadLogo(CryptoExchangeType exchangeType)
    {
        byte[]? png = ReadResource($"Exchanges.{LogoName(exchangeType)}.png");
        return png == null ? null : SKImage.FromEncodedData(png);
    }

    private static byte[]? ReadResource(string name)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + name);
        if (stream == null)
            return null;
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>
    /// The part of the base icon that is not empty. The artwork leaves a margin of about a fifth of
    /// the picture (the coin with its magnifying glass fills 79% by 84%), which is a lot when only
    /// 16 pixels are available; the other icons in a task bar fill theirs.
    /// </summary>
    private static SKRect ArtworkBounds(SKImage baseImage)
    {
        using SKBitmap bitmap = SKBitmap.FromImage(baseImage);
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha <= EmptyAlpha)
                    continue;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }

        // An empty or missing picture: use all of it
        if (right < left || bottom < top)
            return new SKRect(0, 0, bitmap.Width, bitmap.Height);
        return new SKRect(left, top, right + 1, bottom + 1);
    }

    /// <summary>
    /// One size of the icon. Up to <see cref="SmallIconSize"/> the artwork is cropped to what it
    /// holds and drawn as large as the picture allows, in the top left corner so the badge in the
    /// bottom left corner sits over as little of it as possible; larger sizes show the whole picture
    /// with its margin, as the artwork was drawn.
    /// </summary>
    private static SKImage Render(SKImage baseImage, SKRect artwork, SKImage? logo, int size)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Mipmaps keep the large pictures sharp at 16 pixels instead of aliased.
        SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);
        if (size <= SmallIconSize)
        {
            float scale = size / MathF.Max(artwork.Width, artwork.Height);
            canvas.DrawImage(baseImage, artwork, new SKRect(0, 0, artwork.Width * scale, artwork.Height * scale), sampling);
        }
        else
        {
            canvas.DrawImage(baseImage, new SKRect(0, 0, size, size), sampling);
        }

        if (logo != null)
        {
            float diameter = BadgeDiameter(size);
            float radius = diameter / 2;
            float centerX = radius;
            float centerY = size - radius;

            // A light ring separates the badge from the coin underneath and from a dark task bar. At
            // least one pixel, or it disappears in the smallest sizes.
            float ring = MathF.Max(1, MathF.Round(size / 24f));
            using SKPaint ringPaint = new() { IsAntialias = true, Color = new SKColor(255, 255, 255, 235) };
            canvas.DrawCircle(centerX, centerY, radius, ringPaint);

            float inner = radius - ring;
            using SKPath clip = new();
            clip.AddCircle(centerX, centerY, inner);
            canvas.Save();
            canvas.ClipPath(clip, SKClipOperation.Intersect, true);
            canvas.DrawImage(logo, new SKRect(centerX - inner, centerY - inner, centerX + inner, centerY + inner), sampling);
            canvas.Restore();
        }

        return surface.Snapshot();
    }

    private static byte[] EncodePng(SKImage picture)
    {
        using SKData data = picture.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// A picture as the bitmap an .ico has always held: 32 bit BGRA rows stored bottom-up, a height
    /// in the header of twice the picture, and behind it a 1 bit mask of the transparent pixels for
    /// readers that ignore the alpha channel.
    /// </summary>
    private static byte[] EncodeBitmap(SKImage picture)
    {
        int size = picture.Width;
        using SKBitmap pixels = new(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Unpremul));
        if (!picture.ReadPixels(pixels.Info, pixels.GetPixels(), pixels.RowBytes, 0, 0))
            throw new InvalidOperationException("The icon picture cannot be read back");
        ReadOnlySpan<byte> bgra = pixels.GetPixelSpan();

        int rowBytes = size * 4;
        int maskRowBytes = (size + 31) / 32 * 4; // every mask row is padded to 32 bits
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write(40); // header size
        writer.Write(size); // width
        writer.Write(size * 2); // height: picture and mask
        writer.Write((ushort)1); // colour planes
        writer.Write((ushort)32); // bits per pixel
        writer.Write(0); // no compression
        writer.Write(size * (rowBytes + maskRowBytes));
        writer.Write(0); // horizontal resolution
        writer.Write(0); // vertical resolution
        writer.Write(0); // colours used
        writer.Write(0); // important colours

        for (int y = size - 1; y >= 0; y--)
            writer.Write(bgra.Slice(y * pixels.RowBytes, rowBytes));

        byte[] maskRow = new byte[maskRowBytes];
        for (int y = size - 1; y >= 0; y--)
        {
            Array.Clear(maskRow);
            for (int x = 0; x < size; x++)
            {
                if (bgra[y * pixels.RowBytes + x * 4 + 3] == 0)
                    maskRow[x / 8] |= (byte)(0x80 >> (x % 8));
            }
            writer.Write(maskRow);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>The diameter of the badge, ring included, in an icon of <paramref name="size"/> pixels.</summary>
    internal static float BadgeDiameter(int size)
    {
        float fraction = size <= SmallIconSize ? SmallBadgeFraction : BadgeFraction;
        return MathF.Max(8, MathF.Round(size * fraction));
    }

    /// <summary>
    /// An .ico with PNG images, which Windows understands since Vista (and Avalonia and Photino
    /// hand the file to Windows as is).
    /// <para>
    /// Since 30-10-2026 only the largest picture is a PNG and the others are bitmaps (see
    /// <see cref="PngSize"/>); the directory is the same for both.
    /// </para>
    /// </summary>
    private static byte[] WriteIco(List<(int Size, byte[] Data)> entries)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write((ushort)0); // reserved
        writer.Write((ushort)1); // type: icon
        writer.Write((ushort)entries.Count);

        int offset = 6 + 16 * entries.Count;
        foreach (var (size, data) in entries)
        {
            writer.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0); // no palette
            writer.Write((byte)0); // reserved
            writer.Write((ushort)1); // colour planes
            writer.Write((ushort)32); // bits per pixel
            writer.Write(data.Length);
            writer.Write(offset);
            offset += data.Length;
        }
        foreach (var (_, data) in entries)
            writer.Write(data);

        writer.Flush();
        return stream.ToArray();
    }
}
