using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Telegram;

// A minimal client for the Telegram Bot API (https://core.telegram.org/bots/api).
//
// Why our own instead of a package: the scanner uses exactly three calls (getMe, getUpdates and
// sendMessage) out of the ~80 the Bot API offers. Every major version of the Telegram.Bot package
// renamed those three, so an update of a package we barely use kept costing compile errors. The
// HTTP interface underneath does not break - Telegram only adds fields - so talking to it directly
// removes a dependency and the breaking changes that came with it.
//
// The wire format is simple: post json to https://api.telegram.org/bot<token>/<method> and get back
// {"ok":true,"result":...} or
// {"ok":false,"error_code":429,"description":"...","parameters":{"retry_after":5}}


/// <summary>
/// The kind of content in a message. The Bot API does not send this as a field, it follows from
/// which of the optional message fields is filled in.
/// </summary>
public enum TelegramMessageType
{
    Unknown,
    Text,
    Photo,
}


/// <summary>
/// How Telegram should interpret the markup in the text of a message.
/// </summary>
public enum TelegramParseMode
{
    None,
    Html,
    Markdown,
    MarkdownV2,
}


public class TelegramUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}


public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}


public class TelegramPhotoSize
{
    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}


public class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    /// <summary>Unix time (seconds) the message was sent.</summary>
    [JsonPropertyName("date")]
    public long Date { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat Chat { get; set; } = new();

    [JsonPropertyName("from")]
    public TelegramUser? From { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("photo")]
    public TelegramPhotoSize[]? Photo { get; set; }

    /// <summary>
    /// Derived, the Bot API has no such field: a message carries text, or a photo, or something we
    /// do not look at.
    /// </summary>
    [JsonIgnore]
    public TelegramMessageType Type
    {
        get
        {
            if (Text != null)
                return TelegramMessageType.Text;
            if (Photo != null)
                return TelegramMessageType.Photo;
            return TelegramMessageType.Unknown;
        }
    }
}


public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public int Id { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }

    [JsonPropertyName("edited_message")]
    public TelegramMessage? EditedMessage { get; set; }

    [JsonPropertyName("channel_post")]
    public TelegramMessage? ChannelPost { get; set; }
}


/// <summary>
/// The "parameters" object Telegram adds to a rejection, telling us how to recover from it.
/// </summary>
public class TelegramResponseParameters
{
    /// <summary>Seconds to wait before repeating the request (sent with error 429).</summary>
    [JsonPropertyName("retry_after")]
    public int? RetryAfter { get; set; }

    [JsonPropertyName("migrate_to_chat_id")]
    public long? MigrateToChatId { get; set; }
}


/// <summary>
/// Telegram answered, but said no ("ok":false). The message is Telegram's own description, so that
/// something like "Too Many Requests: retry after 5" reaches the log unchanged.
/// </summary>
public class TelegramApiException : Exception
{
    public int ErrorCode { get; }
    public TelegramResponseParameters? Parameters { get; }

    public TelegramApiException(int errorCode, string message, TelegramResponseParameters? parameters) : base(message)
    {
        ErrorCode = errorCode;
        Parameters = parameters;
    }
}


/// <summary>
/// The envelope every Bot API answer arrives in. Internal, the tests reach it through the
/// InternalsVisibleTo in the project file.
/// </summary>
internal class TelegramApiResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public TelegramResponseParameters? Parameters { get; set; }
}


public class TelegramApiClient
{
    /// <summary>
    /// Long polling: how long Telegram may hold the getUpdates request open while it waits for a
    /// command to arrive. Zero answers immediately, which is what the old code asked for - together
    /// with the half second pause in the loop that is two requests per second per scanner, and that
    /// is where the nightly 429 came from. At 25 seconds the same loop makes about 140 requests an
    /// hour and a typed command still arrives at once, because Telegram answers the moment it has
    /// something to hand over.
    /// </summary>
    public const int PollTimeoutSeconds = 25;

    // One HttpClient for the whole process, for the usual reason: a new one per call runs the
    // machine out of sockets. Its timeout has to sit well above PollTimeoutSeconds, otherwise every
    // single poll would end in a cancelled request.
    private static readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(PollTimeoutSeconds + 60)
    };

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string baseUrl;


    public TelegramApiClient(string token, string apiUrl = "https://api.telegram.org")
    {
        // The token is part of the url, so a mistyped one comes back from Telegram as a plain
        // "Not Found" that says nothing about what is wrong. The settings screen leans on this check
        // to tell a wrong token from a refused one. A token looks like 1234567890:AAF-abc..
        int colon = token.IndexOf(':');
        if (colon <= 0 || colon == token.Length - 1 || !token[..colon].All(char.IsAsciiDigit))
            throw new ArgumentException("a telegram token looks like 1234567890:AAF-abc.., this one does not", nameof(token));

        baseUrl = apiUrl.TrimEnd('/') + "/bot" + token + "/";
    }


    /// <summary>
    /// Post one Bot API method and unpack the envelope Telegram wraps every answer in.
    /// </summary>
    private async Task<T> SendRequestAsync<T>(string method, Dictionary<string, object?>? parameters, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            baseUrl + method, parameters ?? [], jsonOptions, cancellationToken);

        // A rejection carries a json body as well (with the error code and the retry_after), so read
        // it whatever the http status says and only fall back on that status when it is not json.
        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        TelegramApiResponse<T>? answer = null;
        try
        {
            answer = JsonSerializer.Deserialize<TelegramApiResponse<T>>(content, jsonOptions);
        }
        catch (JsonException)
        {
            // Not json at all - a proxy or a gateway answered instead of Telegram
        }

        if (answer == null)
            throw new TelegramApiException((int)response.StatusCode,
                method + ": unexpected answer from Telegram (http " + (int)response.StatusCode + ")", null);

        if (!answer.Ok)
            throw new TelegramApiException(answer.ErrorCode,
                answer.Description ?? method + ": request refused (http " + (int)response.StatusCode + ")", answer.Parameters);

        if (answer.Result == null)
            throw new TelegramApiException(answer.ErrorCode, method + ": empty result", answer.Parameters);

        return answer.Result;
    }


    /// <summary>
    /// getMe, used to verify that the token is accepted before the polling loop starts.
    /// </summary>
    public async Task<TelegramUser> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<TelegramUser>("getMe", null, cancellationToken);
    }


    /// <summary>
    /// getUpdates, the commands typed into the chat. Everything below <paramref name="offset"/> is
    /// confirmed to Telegram and will not be handed out again.
    /// </summary>
    public async Task<TelegramUpdate[]> GetUpdatesAsync(int offset, int timeoutSeconds = PollTimeoutSeconds, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object?> parameters = new()
        {
            { "offset", offset },
            { "timeout", timeoutSeconds },
        };
        return await SendRequestAsync<TelegramUpdate[]>("getUpdates", parameters, cancellationToken);
    }


    /// <summary>
    /// sendMessage. Telegram refuses anything above 4096 characters, that limit is not handled here.
    /// </summary>
    public async Task<TelegramMessage> SendTextMessageAsync(string chatId, string text,
        TelegramParseMode parseMode = TelegramParseMode.None, bool disableWebPagePreview = false,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, object?> parameters = new()
        {
            { "chat_id", chatId },
            { "text", text },
        };

        if (parseMode != TelegramParseMode.None)
            parameters.Add("parse_mode", ParseModeText(parseMode));

        if (disableWebPagePreview)
        {
            // disable_web_page_preview is the retired spelling of this, Telegram still accepts it,
            // but the documented field since Bot API 7.0 is link_preview_options.
            parameters.Add("link_preview_options", new Dictionary<string, object?> { { "is_disabled", true } });
        }

        return await SendRequestAsync<TelegramMessage>("sendMessage", parameters, cancellationToken);
    }


    public async Task<TelegramMessage> SendTextMessageAsync(long chatId, string text,
        TelegramParseMode parseMode = TelegramParseMode.None, bool disableWebPagePreview = false,
        CancellationToken cancellationToken = default)
    {
        return await SendTextMessageAsync(chatId.ToString(), text, parseMode, disableWebPagePreview, cancellationToken);
    }


    private static string ParseModeText(TelegramParseMode parseMode)
    {
        if (parseMode == TelegramParseMode.Html)
            return "HTML";
        if (parseMode == TelegramParseMode.MarkdownV2)
            return "MarkdownV2";
        return "Markdown";
    }
}
