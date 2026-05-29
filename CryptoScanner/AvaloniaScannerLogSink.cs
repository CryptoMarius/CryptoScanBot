using Avalonia.Logging;

using CryptoScanner.Core.Core;

namespace CryptoScanner;

/// <summary>
/// Avalonia <see cref="ILogSink"/> that forwards every Avalonia internal log event to
/// <see cref="ScannerLog.Logger"/> (NLog). Captures binding errors, XAML loader failures,
/// layout warnings and other diagnostics that <c>.LogToTrace()</c> would only put into
/// System.Diagnostics.Trace.
///
/// Filtered at Warning+ by default to avoid flooding the log with Debug/Verbose noise.
/// </summary>
internal sealed class AvaloniaScannerLogSink : ILogSink
{
    private readonly LogEventLevel _minLevel;

    public AvaloniaScannerLogSink(LogEventLevel minLevel = LogEventLevel.Warning)
    {
        _minLevel = minLevel;
    }

    public bool IsEnabled(LogEventLevel level, string area) => level >= _minLevel;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        if (!IsEnabled(level, area))
            return;
        Write(level, area, source, messageTemplate);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
            return;

        // Avalonia uses {0}, {1} positional placeholders; string.Format handles those.
        // Named placeholders ({Property}) would throw — we fall back to the raw template.
        string msg;
        try
        {
            msg = string.Format(messageTemplate, propertyValues);
        }
        catch
        {
            msg = messageTemplate;
        }

        Write(level, area, source, msg);
    }

    private static void Write(LogEventLevel level, string area, object? source, string message)
    {
        string prefix = source != null
            ? $"[Avalonia/{area}] {source.GetType().Name}: "
            : $"[Avalonia/{area}] ";
        string fullMessage = prefix + message;

        switch (level)
        {
            case LogEventLevel.Fatal:
                ScannerLog.Logger.Fatal(fullMessage);
                break;
            case LogEventLevel.Error:
                ScannerLog.Logger.Error(fullMessage);
                break;
            case LogEventLevel.Warning:
                ScannerLog.Logger.Warn(fullMessage);
                break;
            case LogEventLevel.Information:
                ScannerLog.Logger.Info(fullMessage);
                break;
            case LogEventLevel.Debug:
            case LogEventLevel.Verbose:
            default:
                ScannerLog.Logger.Trace(fullMessage);
                break;
        }
    }
}
