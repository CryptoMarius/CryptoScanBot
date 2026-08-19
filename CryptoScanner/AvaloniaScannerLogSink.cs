using Avalonia.Logging;

using CryptoScanner.Core.Core;

using System.Text;

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

        // Avalonia uses both {0}, {1} positional and {Property} named placeholders, and fills them in
        // argument order. string.Format only understands the positional form and throws on the named one,
        // after which the old fallback wrote the raw template — that is why the log was full of
        // "binding {Property} to {Expression} ... {Message}" lines without a single usable value.
        Write(level, area, source, Format(messageTemplate, propertyValues));
    }


    /// <summary>
    /// Replaces every placeholder in <paramref name="messageTemplate"/> with the next value from
    /// <paramref name="propertyValues"/>, regardless of whether the placeholder is positional ({0}) or
    /// named ({Property}). Doubled braces ({{ and }}) are treated as escapes, as in string.Format.
    /// </summary>
    private static string Format(string messageTemplate, object?[]? propertyValues)
    {
        if (propertyValues == null || propertyValues.Length == 0)
            return messageTemplate;

        StringBuilder builder = new(messageTemplate.Length + 64);
        int valueIndex = 0;

        for (int i = 0; i < messageTemplate.Length; i++)
        {
            char c = messageTemplate[i];

            if (c == '{' && i + 1 < messageTemplate.Length && messageTemplate[i + 1] == '{')
            {
                builder.Append('{');
                i++;
                continue;
            }

            if (c == '}' && i + 1 < messageTemplate.Length && messageTemplate[i + 1] == '}')
            {
                builder.Append('}');
                i++;
                continue;
            }

            if (c == '{')
            {
                int end = messageTemplate.IndexOf('}', i + 1);
                if (end < 0)
                {
                    // Unterminated placeholder, emit the rest as-is
                    builder.Append(messageTemplate, i, messageTemplate.Length - i);
                    break;
                }

                if (valueIndex < propertyValues.Length)
                    builder.Append(propertyValues[valueIndex]?.ToString() ?? "null");
                else
                    builder.Append(messageTemplate, i, end - i + 1); // no value left, keep the placeholder
                valueIndex++;
                i = end;
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Avalonia messages that are demoted to Trace instead of being written at their own level.
    /// <para>
    /// They are not defects and there is nothing on our side to fix, but they do end up in the error
    /// log and in front of users, who then ask about them. Trace keeps them available when you go
    /// looking, and out of sight when you are not.
    /// </para>
    /// <para>
    /// The compositor one is the render thread that did not get its frame committed in time and ticks
    /// on by itself. With twenty scanner windows on one machine that is contention on the graphics
    /// card and nothing else: no candle is lost and the scan does not notice. It was 73 of the 131
    /// error lines on the night of 18/19-08-2026, which put nearly every exchange on "attention" for
    /// something nobody would act on.
    /// </para>
    /// <para>
    /// Add to this list only for a message that is understood AND outside our control - it is meant to
    /// keep the error log meaningful, not to make it quiet.
    /// </para>
    /// </summary>
    private static readonly string[] DemotedToTrace =
    [
        "RequestCommitAsync timed out",
    ];

    private static void Write(LogEventLevel level, string area, object? source, string message)
    {
        string prefix = source != null
            ? $"[Avalonia/{area}] {source.GetType().Name}: "
            : $"[Avalonia/{area}] ";
        string fullMessage = prefix + message;

        foreach (string fragment in DemotedToTrace)
        {
            if (message.Contains(fragment, StringComparison.Ordinal))
            {
                ScannerLog.Logger.Trace(fullMessage);
                return;
            }
        }

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
