namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>chatid: the id of this chat, for the Telegram settings of the scanner.</summary>
public class TelegramCommandChatId : TelegramCommand
{
    public override string Name => "chatid";
    public override string Description => "show the id of this chat (for the settings)";

    // Open to every chat: a new chat has to be able to find its id before it is configured
    public override bool OnlyOwnChat => false;

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        context.Output.AppendLine("ChatId: " + context.ChatId.ToString());
        return Task.CompletedTask;
    }
}
