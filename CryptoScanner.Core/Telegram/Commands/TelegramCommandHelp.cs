using System.Net;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>help: every command with its arguments, built from <see cref="TelegramCommand.Commands"/>.</summary>
public class TelegramCommandHelp : TelegramCommand
{
    public override string Name => "help";
    public override string Description => "this help screen";

    /// <summary>The heading of each group in the help.</summary>
    private static string Heading(TelegramCommandGroup group) => group switch
    {
        TelegramCommandGroup.Overview => "\U0001F4CB Overview",
        TelegramCommandGroup.Trading => "⚙️ Trader and signals",
        TelegramCommandGroup.Market => "\U0001F4C8 Market",
        _ => "\U0001F6E0 Scanner",
    };

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        // HTML: a bold heading per group, and "/status" can be tapped in Telegram to send it. A name
        // Telegram does not accept as a command (".") is shown as code instead, so it is not a link.
        context.Html = true;
        var output = context.Output;

        //foreach (TelegramCommand command in Commands)
        //{
        //    string usage = command.Arguments == "" ? command.Name : $"{command.Name} {command.Arguments}";
        //    context.Output.AppendLine($"{usage} - {command.Description}");
        //}
        foreach (TelegramCommandGroup group in Enum.GetValues<TelegramCommandGroup>())
        {
            List<TelegramCommand> commands = Commands.Where(c => c.Group == group).ToList();
            if (commands.Count == 0)
                continue;

            if (output.Length > 0)
                output.AppendLine();
            output.AppendLine($"<b>{WebUtility.HtmlEncode(Heading(group))}</b>");

            foreach (TelegramCommand command in commands)
            {
                string name = WebUtility.HtmlEncode(command.Name);
                string usage = IsMenuName(command.Name) ? "/" + name : $"<code>{name}</code>";
                if (command.Arguments != "")
                    usage += $" <i>{WebUtility.HtmlEncode(command.Arguments)}</i>";
                output.AppendLine($"{usage} – {WebUtility.HtmlEncode(command.Description)}");
            }
        }
        return Task.CompletedTask;
    }
}
