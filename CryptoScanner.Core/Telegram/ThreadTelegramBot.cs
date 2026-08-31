using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper;

using System.Text;

namespace CryptoScanner.Core.Telegram;


// Talk to BotFather on Telegram
// (find the user and)
// ask him: /newbot
//
// This command will create a new bot for you
// It will ask two things:
// 1: Choose a name for your bot: <BotName>
// 2: Choose a username for your bot: <BotName>Bot
//
// Copy the Token (like:1234567890:12345678901234567890123456789012345)
// Paste it into the "Telegram Token" field of the Scanner telegram settings

// Go the suggested Chat BotFather created
// Type ChatId (a command of the Scanner)
// Copy the ChatId (710219603)
// Paste it into the "Telegram ChatId" field of the Scanner telegram settings
// Press the test button, that should work (I hope)
//
// Lots of other commands available
//

public static class ThreadTelegramBot
{
    public static string Token { get; set; } = "";
    public static string ChatId { get; set; } = "";
    private static ThreadTelegramBotInstance? bot;
    private static Task? botTask;

    /// <summary>
    /// Serialises <see cref="Start"/> and <see cref="StopAsync"/>: one starter at a time.
    /// </summary>
    /// <remarks>
    /// Start is reached from two places that both fire at startup without anyone awaiting them -
    /// ThreadLoadData hands it to Task.Run, and ScannerSession.ApplyConfigurationAsync is itself
    /// started fire-and-forget by the ui - and between the "is there a bot already" check and the
    /// assignment sits an await on PrepareAsync, a network round trip to Telegram. Without this lock
    /// both callers could get past that check, both build an instance, and the second would
    /// overwrite the first. Nothing held a reference to the loser any more, so StopAsync could never
    /// reach it and it kept polling until the process ended. Telegram answers a second getUpdates on
    /// the same token with "Conflict: terminated by other getUpdates request", which is what filled
    /// the HyperLiquid Perpetual error log with three to four lines a minute from 31-08-2026 07:43
    /// onwards, straight through a restart of the bot at 07:59.
    /// </remarks>
    private static readonly SemaphoreSlim startStopLock = new(1, 1);

    /// <summary>Counts the polling loops this process has started, for the log line in <see cref="StartInternalAsync"/>.</summary>
    private static int botInstanceNumber;

    /// <summary>
    /// Whether the polling loop is running. The settings screens put this on their Start/Stop button.
    /// </summary>
    public static bool IsRunning => bot != null;


    /// <summary>
    /// Start the bot. Returns false when Telegram refuses the token, which is what the settings
    /// screen wants to know; the reason is in the log.
    /// </summary>
    public static async Task<bool> Start(string token, string chatId)
    {
        await startStopLock.WaitAsync();
        try
        {
            return await StartInternalAsync(token, chatId);
        }
        finally
        {
            startStopLock.Release();
        }
    }


    /// <summary>
    /// The body of <see cref="Start"/>, called with <see cref="startStopLock"/> already held.
    /// </summary>
    private static async Task<bool> StartInternalAsync(string token, string chatId)
    {
        // herstart?
        if (bot != null)
            await StopInternalAsync();

        //GlobalData.AddTextToLogTab(string.Format("Start telegram handler"));
        Token = token;
        ChatId = chatId;

        ThreadTelegramBotInstance instance = new();
        if (!await instance.PrepareAsync(token))
            return false;
        bot = instance;

        // The number is what makes a double start visible: two of these lines on one process start
        // means two polling loops, and that is the shape the getUpdates conflicts came in. It stays
        // in - one line per start is nothing, and without it the next occurrence is guesswork again.
        botInstanceNumber++;
        GlobalData.AddTextToLogTab($"Telegram polling loop {botInstanceNumber} started");

        // Everything AddTextToTelegram() gathers travels over this event: the startup message, the
        // position notifications, the Altrady webhook and the test button of the settings screen.
        // Nothing has been subscribed to it since the winforms ui was removed in f3cb6623
        // (11-12-2025), so all of that went nowhere. Subscribing here and dropping it in StopAsync
        // keeps the subscription alive exactly as long as the bot is.
        GlobalData.LogToTelegram -= SendMessage;
        GlobalData.LogToTelegram += SendMessage;

        // ExecuteAsync does not return until the bot is stopped, so it gets a task of its own.
        // Awaiting it here hung the caller: ApplyConfigurationAsync awaits this method, and never
        // reached its ScheduleRefresh and the messages behind it whenever the token had changed.
        botTask = Task.Run(async () =>
        {
            try
            {
                await instance.ExecuteAsync(token);
            }
            finally
            {
                // The loop can also end on its own (a withdrawn token, an error it cannot recover
                // from). Clearing the state here keeps IsRunning honest for the button.
                if (ReferenceEquals(bot, instance))
                {
                    bot = null;
                    GlobalData.LogToTelegram -= SendMessage;
                }
            }
        });
        return true;
    }


    /// <summary>
    /// Stop the bot and wait for the polling loop to end. ScannerSession hands this method to
    /// Task.Run, which is why the blocking form is kept next to <see cref="StopAsync"/>.
    /// </summary>
    public static void Stop()
    {
        StopAsync().GetAwaiter().GetResult();
    }


    public static async Task StopAsync()
    {
        await startStopLock.WaitAsync();
        try
        {
            await StopInternalAsync();
        }
        finally
        {
            startStopLock.Release();
        }
    }


    /// <summary>
    /// The body of <see cref="StopAsync"/>, called with <see cref="startStopLock"/> already held.
    /// <see cref="StartInternalAsync"/> uses this one, because taking the lock a second time on the
    /// same call chain would deadlock.
    /// </summary>
    private static async Task StopInternalAsync()
    {
        ThreadTelegramBotInstance? instance = bot;
        if (instance == null)
            return;

        //GlobalData.AddTextToLogTab(string.Format("Stop telegram handler"));
        bot = null;
        GlobalData.LogToTelegram -= SendMessage;
        instance.Stop();

        Task? running = botTask;
        botTask = null;
        if (running != null)
        {
            try
            {
                // Cancelling aborts the poll that is in flight, so this returns straight away. The
                // loop logs its own failures, there is nothing to pass on here.
                await running;
            }
            catch (Exception)
            {
            }
        }
    }


    public static async void SendMessage(string text)
    {
        if (bot == null || text == "" || ChatId == "")
            return;
        await bot.SendMessage(text);
    }


    async public static void SendSignal(CryptoSignal signal)
    {
        if (bot == null || signal == null || ChatId == "")
            return;
        await bot.SendSignal(signal);
    }
}



public class ThreadTelegramBotInstance
{
    private static int offset;
    private TelegramApiClient? bot;
    public CancellationTokenSource cancellationToken = new();

    public void Stop()
    {
        cancellationToken.Cancel();
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="text"></param>
    public async Task SendMessage(string text)
    {
        if (bot == null || text == "" || ThreadTelegramBot.ChatId == "")
            return;


        try
        {
            //var DisableLink = new LinkPreviewOptions { IsDisabled = true };
            //await bot.SendMessage(ThreadTelegramBot.ChatId, text, parseMode: TelegramParseMode.Html, linkPreviewOptions: DisableLink);
            await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, text, parseMode: TelegramParseMode.Html, disableWebPagePreview: true);
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
        }
    }


    public async Task SendSignal(CryptoSignal signal)
    {
        if (bot == null || signal == null || ThreadTelegramBot.ChatId == "")
            return;

        try
        {
            string text = TelegramGenerateSignalText.Execute(signal);
            if (text != string.Empty)
                _ = await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, text, parseMode: TelegramParseMode.Html, disableWebPagePreview: true);
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
        }
    }


    private static void CommandShowProfits(StringBuilder stringbuilder)
    {
        decimal sumInvested = 0;
        decimal sumProfit = 0;
        decimal sumPositions = 0;

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from position " +
            "where CloseTime >= @fromDate and status=2 and exchangeid=@exchangeid",
            new { fromDate = DateTime.Today.ToUniversalTime(), exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            sumPositions++;
            sumProfit += position.Profit;
            sumInvested += position.Invested;
        }

        decimal percentage = 0;
        if (sumInvested > 0)
            percentage = 100 * sumProfit / sumInvested;

        stringbuilder.AppendLine($"{sumPositions} positions, invested {sumInvested:N2}, profits {sumProfit:N2}, {percentage:N2}%");
    }


    /// <summary>
    /// Build the client and ask Telegram who we are. False means the token was refused - that is the
    /// answer the settings screen is after, and the reason is in the log.
    /// </summary>
    public async Task<bool> PrepareAsync(string token)
    {
        if (token == "")
            return false;

        // System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        //    // Extra parameters vanwege ambigious constructor (die ik niet geheel kon volgen)
        try
        {
            // Both of these have to end up in the log instead of on the caller's doorstep, because
            // two settings screens await this by way of ThreadTelegramBot.Start: the client refuses
            // a token that is not shaped like one, and getMe says whether Telegram accepts it.
            bot = new(token); //, "https://api.telegram.org/bot", "https://api.telegram.org/file/bot"
            //SendMessage("Started telegram bot!");

            var me = await bot.GetMeAsync();
            //GlobalData.AddTextToLogTab($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
            //return; //t'ding crasht en is niet fijn
            return true;
        }
        catch (Exception error)
        {
            bot = null;
            // One line, no stack trace - AddErrorToLogTab writes to the logger itself
            GlobalData.AddErrorToLogTab($"ERROR telegram thread {error.Message}");
            return false;
        }
    }


    public async Task ExecuteAsync(string token)
    {
        if (token == "")
            return;

        // Bij het testen staat vaak de scanner aan, daarom bij sql telegram ff uit

        // Started through ThreadTelegramBot.Start the client is already built and the token already
        // checked; this is here for a caller that skips that step.
        if (bot == null && !await PrepareAsync(token))
            return;
        // PrepareAsync either filled the client in or returned false, so it is there by now.
        // Spelled out because the compiler cannot see that through the condition above.
        if (bot == null)
            return;

        try
        {
            //// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            //ReceiverOptions receiverOptions = new()
            //{
            //    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            //};

            //botClient.StartReceiving(
            //    updateHandler: HandleUpdateAsync,
            //    pollingErrorHandler: HandlePollingErrorAsync,
            //    receiverOptions: receiverOptions,
            //    cancellationToken: cts.Token
            //);

            // Telegram rejects getUpdates with 429 (and answers 502 while it is busy). Without a
            // pause the 500 ms retry at the bottom of the loop turns one rejection into a burst of
            // them, which is what the log shows every night around 03:10. This grows the pause on
            // each consecutive failure and is reset as soon as a call succeeds.
            // The first pause is five seconds and not two: nothing here is urgent (this loop only
            // collects the commands typed into the chat), and the night of 19/20-08-2026 showed the
            // shape of the problem - a 429 at 03:10:38 and a 502 seven seconds later, so the two
            // second pause put us back at Telegram's door while it was still busy. Doubling from
            // five means 5, 10, 20, 30 and the whole episode costs one log line instead of two.
            int backOffSeconds = 0;
            const int backOffSecondsFirst = 5;
            const int backOffSecondsMaximum = 30;

            // Dat moet ook nog eens wat netter met een CT
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //SendMessage("hello over there!");
                    TelegramUpdate[] updates = await bot.GetUpdatesAsync(offset, cancellationToken: cancellationToken.Token);
                    backOffSeconds = 0;

                    foreach (TelegramUpdate update in updates)
                    {
                        //// Only process Message updates: https://core.telegram.org/bots/api#message
                        if (update.Message == null)
                            continue;
                        //// Wat is dit voor een syntax,
                        //// Is {} message een type? queue?
                        //// Blijkbaar, maar hoe dan?
                        //if (update.Message is not { } message)
                        //    continue;
                        //// Only process text messages
                        //if (message.Text is not { } messageText)
                        //    continue;

                        //teleGramCount++;
                        offset = update.Id + 1;
                        try
                        {
                            switch (update.Message.Type)
                            {
                                case TelegramMessageType.Text:
                                    {
                                        bool showInHtml = false;
                                        StringBuilder stringBuilder = new();
                                        string arguments = "";
                                        if (update.Message.Text != null)
                                            arguments = update.Message.Text.ToUpper();

                                        string command = "";
                                        string[] parameters = arguments.Split(' ');
                                        if (parameters.Length > 0)
                                            command = parameters[0].Trim().ToUpper();

                                        if (command == ".")
                                        {
                                            TelegramShowBarometer.ShowBarometer(arguments, stringBuilder);
                                            stringBuilder.AppendLine();
                                            GlobalData.ActiveExchange!.Data.PositionList.ShowPositions(stringBuilder);
                                            stringBuilder.AppendLine();
                                            CommandShowProfits(stringBuilder);
                                            stringBuilder.AppendLine();
                                            Helper.ShowAssets(GlobalData.ActiveExchange!, stringBuilder, out decimal _, out decimal _);
                                            stringBuilder.AppendLine();
                                            TelegramShowValue.ShowValue(command, stringBuilder);
                                        }
                                        else
                                            if (command == "STATUS")
                                                TelegramShowStatus.ShowStatus(command, stringBuilder);
                                            else if (command == "VALUE")
                                                TelegramShowValue.ShowValue(command, stringBuilder);
                                            else if (command == "RESET")
                                                TelegramResetScanner.Execute(command, stringBuilder);
                                            else if (command == "CALCULATEZONES")
                                                TelegramCalculateZones.Execute(command, stringBuilder);
                                            else if (command == "ZONES")
                                                showInHtml = TelegramShowZones.Execute(arguments, stringBuilder);
                                            else if (command == "POSITIONS")
                                                GlobalData.ActiveExchange!.Data.PositionList.ShowPositions(stringBuilder);
                                            else if (command == "PROFITS")
                                                CommandShowProfits(stringBuilder);
                                            else if (command == "SLOTS")
                                                TelegramBotSlots.Execute(arguments, stringBuilder);
                                            else if (command == "START")
                                                TelegramBotStart.Execute(arguments, stringBuilder);
                                            else if (command == "SIGNALSTART")
                                                TelegramBotStart.Execute("command signals", stringBuilder);
                                            //else if (command == "ADVICESTARTS")
                                            //    StopBot("command advice", stringBuilder);
                                            //else if (command == "BALANCESTART")
                                            //    TelegramBotStart.Execute("command balancing", stringBuilder);
                                            else if (command == "STOP")
                                                TelegramBotStop.Execute(arguments, stringBuilder);
                                            else if (command == "SIGNALSTOP")
                                                TelegramBotStop.Execute("command signals", stringBuilder);
                                            //else if (command == "ADVICESTOP")
                                            //    StopBot("command advice", stringBuilder);
                                            //else if (command == "BALANCESTOP")
                                            //    StopBot("command balancing", stringBuilder);
                                            else if (command == "BAROMETER")
                                                TelegramShowBarometer.ShowBarometer(arguments, stringBuilder);
                                            else if (command == "ASSETS")
                                            {
                                                AssetTools.FetchAssets(GlobalData.ActiveExchange!);
                                                Helper.ShowAssets(GlobalData.ActiveExchange!, stringBuilder, out decimal _, out decimal _);
                                            }
                                            else if (command == "TREND")
                                                await TelegramShowTrend.ShowTrendAsync(arguments, GlobalData.Settings.Trend.Primary, stringBuilder);
                                            else if (command == "HELP")
                                                TelegramShowHelp.ShowHelp(stringBuilder);
                                            else if (command == "CHATID")
                                                stringBuilder.AppendLine("ChatId: " + update.Message.Chat.Id.ToString());
                                            else stringBuilder.Append("Not a command..");


                                        string s = stringBuilder.ToString();
                                        if (s != "")
                                        {
                                            if (showInHtml)
                                                await bot.SendTextMessageAsync(update.Message.Chat.Id, s, parseMode: TelegramParseMode.Html, disableWebPagePreview: true);
                                            else
                                                await bot.SendTextMessageAsync(update.Message.Chat.Id, s);
                                        }
                                    }
                                    break;

                                    //    case TelegramMessageType.Photo:
                                    //        {
                                    //            // geen idee, niet belangrijk in een bot denk ik
                                    //            // await ProcessPhotoMessage(update.Message);
                                    //        }
                                    //        break;
                            }


                        }
                        catch (HttpRequestException error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            ScannerLog.Logger.Error(error.Message);
                            await Task.Delay(5000);
                        }
                        catch (TelegramApiException error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            ScannerLog.Logger.Error(error.Message);
                            await Task.Delay(5000);
                        }
                        catch (Exception error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            ScannerLog.Logger.Error(error, "");
                            await Task.Delay(2500);
                        }

                    }
                }
                catch (TelegramApiException error)
                {
                    // Stupid Telegram is not playing nice
                    // Telegram tells us how long to wait ("retry after 5"); honour that value when it is
                    // present, otherwise back off ourselves. Without this the same request is repeated
                    // 500 ms later and the rejections simply pile up.
                    int waitSeconds = error.Parameters?.RetryAfter ?? 0;
                    if (waitSeconds <= 0)
                    {
                        backOffSeconds = backOffSeconds == 0
                            ? backOffSecondsFirst
                            : Math.Min(backOffSeconds * 2, backOffSecondsMaximum);
                        waitSeconds = backOffSeconds;
                    }
                    // One call, not two: AddErrorToLogTab writes to the logger itself (see GlobalData),
                    // so logging it here as well put every Telegram failure in both log files twice.
                    // The message is simplified to one line on purpose, no stack trace.
                    GlobalData.AddErrorToLogTab($"ERROR telegram thread {error.Message} (waiting {waitSeconds}s)");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Stopping, the while condition takes care of the rest
                    }
                    continue;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Stopping. With long polling the getUpdates request is left open at Telegram,
                    // so cancelling aborts a request that is in flight - that is the normal way out
                    // of this loop and not something to log. A timeout of the http client itself is
                    // a different matter and falls through to the catch below.
                    continue;
                }
                catch (Exception error)
                {
                    // Stupid Telegram is not playing nice
                    //ScannerLog.Logger.Error(error, "");
                    // One call, not two - see the remark above. Simplified to one line on purpose.
                    GlobalData.AddErrorToLogTab($"ERROR telegram thread {error.Message}");
                }
                await Task.Delay(500);
            }
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            // The logger call stays here: this catch is the one that ends the thread, so the stack
            // trace is worth having. AddErrorToLogTab below carries the short version to the log tab.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"ERROR telegram thread {error.Message}");
        }
        GlobalData.AddTextToLogTab("Task Telegram stopped");
    }

}
