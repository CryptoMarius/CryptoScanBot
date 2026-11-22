using CryptoScanner.Core.Core;
using CryptoScanner.Core.Messages;

using System.Text;
using System.Text.RegularExpressions;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>
/// Everything one command gets to work with: the words typed after the command, the chat it came
/// from, and the text that goes back.
/// </summary>
public class TelegramCommandContext
{
    /// <summary>The words after the command, as typed (case kept, empty words removed).</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>The chat the command was typed in.</summary>
    public long ChatId { get; init; }

    /// <summary>The answer. Left empty means nothing is sent back.</summary>
    public StringBuilder Output { get; } = new();

    /// <summary>Send the answer with HTML parse mode (links); plain text otherwise.</summary>
    public bool Html { get; set; }

    /// <summary>The argument at <paramref name="index"/>, or "" when it was not typed.</summary>
    public string Argument(int index) => index < Arguments.Count ? Arguments[index] : "";
}


/// <summary>The heading a command is listed under in the help, in this order.</summary>
public enum TelegramCommandGroup
{
    Overview,
    Trading,
    Market,
    Scanner,
}


/// <summary>The answer to one typed message.</summary>
public record TelegramCommandReply(string Text, bool Html)
{
    public static readonly TelegramCommandReply None = new("", false);
}


/// <summary>
/// Base class of the commands that can be typed into the Telegram chat of the scanner. Each command
/// lives in a unit of its own (TelegramCommand*.cs in this folder) and is listed once in
/// <see cref="Commands"/>; the help text and the command menu Telegram shows are built from that
/// same list, so they cannot drift away from what really exists.
/// </summary>
public abstract partial class TelegramCommand
{
    /// <summary>Telegram refuses a sendMessage longer than this.</summary>
    public const int MaximumMessageLength = 4096;

    /// <summary>What is typed, in lower case and without the leading slash.</summary>
    public abstract string Name { get; }

    /// <summary>The arguments, as shown in the help ("[count]", "symbol").</summary>
    public virtual string Arguments => "";

    /// <summary>One line for the help and for the menu of the bot.</summary>
    public abstract string Description { get; }

    /// <summary>The heading the command is listed under in the help.</summary>
    public virtual TelegramCommandGroup Group => TelegramCommandGroup.Scanner;

    /// <summary>
    /// Only the configured chat may give this command. Anyone who knew the bot's name could otherwise
    /// stop the trader, change the slots or reset the scanner, and got the positions and assets back.
    /// </summary>
    public virtual bool OnlyOwnChat => true;

    /// <summary>Do the work and write the answer into <see cref="TelegramCommandContext.Output"/>.</summary>
    public abstract Task ExecuteAsync(TelegramCommandContext context);


    /// <summary>
    /// Store the settings a command changed and let both scanners know, so the checkboxes and the
    /// traffic lights follow (the same message the buttons on the dashboard send).
    /// </summary>
    protected static void SettingsChanged()
    {
        GlobalData.SaveConfiguration();
        GlobalData.SendMvvmMessage(new StatusesHaveChangedMessage());
    }


    /// <summary>Case-insensitive comparison of a typed word.</summary>
    protected static bool Is(string typed, string word) => string.Equals(typed, word, StringComparison.OrdinalIgnoreCase);


    // ---------------------------------------------------------------------------------------------
    // The list of commands and the dispatching
    // ---------------------------------------------------------------------------------------------

    /// <summary>Every command the bot knows, in the order of the help text.</summary>
    public static IReadOnlyList<TelegramCommand> Commands { get; } =
    [
        new TelegramCommandStatus(),
        new TelegramCommandOverview(),
        new TelegramCommandStart(),
        new TelegramCommandStop(),
        new TelegramCommandSignalStart(),
        new TelegramCommandSignalStop(),
        //else if (command == "ADVICESTARTS")
        //    StopBot("command advice", stringBuilder);
        //else if (command == "BALANCESTART")
        //    TelegramBotStart.Execute("command balancing", stringBuilder);
        //else if (command == "ADVICESTOP")
        //    StopBot("command advice", stringBuilder);
        //else if (command == "BALANCESTOP")
        //    StopBot("command balancing", stringBuilder);
        new TelegramCommandSlots(),
        new TelegramCommandPositions(),
        new TelegramCommandProfits(),
        new TelegramCommandAssets(),
        new TelegramCommandValue(),
        new TelegramCommandBarometer(),
        new TelegramCommandTrend(),
        new TelegramCommandZones(),
        new TelegramCommandCalculateZones(),
        new TelegramCommandReset(),
        new TelegramCommandChatId(),
        new TelegramCommandHelp(),
    ];


    /// <summary>The command with this name, or null.</summary>
    public static TelegramCommand? Find(string name)
    {
        foreach (TelegramCommand command in Commands)
        {
            if (Is(command.Name, name))
                return command;
        }
        return null;
    }


    /// <summary>
    /// Split a typed message into the command and its arguments. Accepts the Telegram habit of a
    /// leading slash and the group form "/status@MyScannerBot"; case does not matter.
    /// </summary>
    public static (string command, string[] arguments) Parse(string? text)
    {
        string[] words = (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return ("", []);

        string command = words[0];
        if (command.Length > 1 && command.StartsWith('/'))
            command = command[1..];
        int at = command.IndexOf('@');
        if (at > 0)
            command = command[..at];

        return (command.ToLowerInvariant(), words[1..]);
    }


    /// <summary>
    /// Execute one typed message and return the answer. A failing command answers with the error
    /// instead of leaving the chat silent; a command from a foreign chat is only logged.
    /// </summary>
    public static async Task<TelegramCommandReply> ExecuteAsync(string? text, long chatId, bool ownChat)
    {
        (string name, string[] arguments) = Parse(text);
        if (name == "")
            return TelegramCommandReply.None;

        TelegramCommand? command = Find(name);
        if (command == null)
        {
            if (!ownChat)
                return TelegramCommandReply.None;
            return new($"Unknown command '{name}', type help for the list", false);
        }

        if (command.OnlyOwnChat && !ownChat)
        {
            GlobalData.AddTextToLogTab($"Telegram command {name} ignored, chat {chatId} is not the configured chat");
            return TelegramCommandReply.None;
        }

        TelegramCommandContext context = new() { Arguments = arguments, ChatId = chatId };
        try
        {
            await command.ExecuteAsync(context);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"Telegram command {name}");
            // Plain text: the message may contain characters HTML parse mode would refuse
            return new($"Error in {name}: {error.Message}", false);
        }

        return new(context.Output.ToString().TrimEnd(), context.Html);
    }


    /// <summary>
    /// Cut a text into pieces Telegram accepts, on line boundaries where possible (so an HTML link,
    /// which never spans two lines here, is not cut in half).
    /// </summary>
    public static List<string> SplitMessage(string text, int maximumLength = MaximumMessageLength)
    {
        List<string> parts = [];
        if (string.IsNullOrEmpty(text))
            return parts;

        StringBuilder part = new();
        foreach (string line in text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'))
        {
            string rest = line;
            // A single line longer than the maximum is cut hard
            while (rest.Length > maximumLength)
            {
                if (part.Length > 0)
                {
                    parts.Add(part.ToString());
                    part.Clear();
                }
                parts.Add(rest[..maximumLength]);
                rest = rest[maximumLength..];
            }

            int needed = part.Length == 0 ? rest.Length : part.Length + 1 + rest.Length;
            if (needed > maximumLength)
            {
                parts.Add(part.ToString());
                part.Clear();
            }
            if (part.Length > 0)
                part.Append('\n');
            part.Append(rest);
        }
        if (part.Length > 0)
            parts.Add(part.ToString());

        // A piece of only white space is refused by Telegram ("message text is empty")
        parts.RemoveAll(p => p.Trim() == "");
        return parts;
    }


    [GeneratedRegex("^[a-z0-9_]{1,32}$")]
    private static partial Regex MenuNameRegex();

    /// <summary>Telegram accepts this as a command name (menu, and a tappable "/name" in a message).</summary>
    protected static bool IsMenuName(string name) => MenuNameRegex().IsMatch(name);

    /// <summary>
    /// The commands for the menu of the bot (setMyCommands). Telegram only accepts names of 1 to 32
    /// characters a-z, 0-9 and underscore, so "." stays out of it.
    /// </summary>
    public static List<(string command, string description)> MenuCommands()
    {
        List<(string, string)> list = [];
        foreach (TelegramCommand command in Commands)
        {
            if (IsMenuName(command.Name))
            {
                string description = command.Description;
                if (command.Arguments != "")
                    description = $"{command.Arguments} - {description}";
                if (description.Length > 256)
                    description = description[..256];
                list.Add((command.Name, description));
            }
        }
        return list;
    }
}
