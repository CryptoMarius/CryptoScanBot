using CryptoScanner.Core.Enums;

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

    private static byte[] Draw(CryptoExchangeType? exchangeType, bool emulator)
    {
        using SKImage baseImage = LoadBaseImage(emulator);
        using SKImage? logo = exchangeType.HasValue ? LoadLogo(exchangeType.Value) : null;

        List<(int Size, byte[] Png)> entries = [];
        foreach (int size in IconSizes)
            entries.Add((size, RenderPng(baseImage, logo, size)));
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

        // No PNG inside: let Skia's own .ico decoder pick an image.
        return SKImage.FromEncodedData(bestPng ?? ico)
            ?? throw new InvalidOperationException("The application icon cannot be decoded");
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

    private static byte[] RenderPng(SKImage baseImage, SKImage? logo, int size)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Mipmaps keep the large pictures sharp at 16 pixels instead of aliased.
        SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);
        canvas.DrawImage(baseImage, new SKRect(0, 0, size, size), sampling);

        if (logo != null)
        {
            float diameter = MathF.Max(8, MathF.Round(size * BadgeFraction));
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

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// An .ico with PNG images, which Windows understands since Vista (and Avalonia and Photino
    /// hand the file to Windows as is).
    /// </summary>
    private static byte[] WriteIco(List<(int Size, byte[] Png)> entries)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write((ushort)0); // reserved
        writer.Write((ushort)1); // type: icon
        writer.Write((ushort)entries.Count);

        int offset = 6 + 16 * entries.Count;
        foreach (var (size, png) in entries)
        {
            writer.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0); // no palette
            writer.Write((byte)0); // reserved
            writer.Write((ushort)1); // colour planes
            writer.Write((ushort)32); // bits per pixel
            writer.Write(png.Length);
            writer.Write(offset);
            offset += png.Length;
        }
        foreach (var (_, png) in entries)
            writer.Write(png);

        writer.Flush();
        return stream.ToArray();
    }
}
