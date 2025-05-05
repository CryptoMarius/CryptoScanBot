using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

using Dapper;

using System.Text;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoScanBot.Core.Telegram;


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


    public static async Task Start(string token, string chatId)
    {
        // herstart?
        if (bot != null)
            Stop();

        //GlobalData.AddTextToLogTab(string.Format("Start telegram handler"));
        Token = token;
        ChatId = chatId;

        bot = new();
        await bot.ExecuteAsync(token);
    }


    public static void Stop()
    {
        if (bot != null)
        {
            //GlobalData.AddTextToLogTab(string.Format("Stop telegram handler"));
            bot.Stop();
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
    private TelegramBotClient? bot;
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
            //await bot.SendMessage(ThreadTelegramBot.ChatId, text, parseMode: ParseMode.Html, linkPreviewOptions: DisableLink);
            await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, text, parseMode: ParseMode.Html, disableWebPagePreview: true);
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
                _ = await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, text, parseMode: ParseMode.Html, disableWebPagePreview: true);
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


    public async Task ExecuteAsync(string token)
    {
        if (token == "")
            return;

        // Bij het testen staat vaak de scanner aan, daatom bij sql telegram ff uit

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        //    // Extra parameters vanwege ambigious constructor (die ik niet geheel kon volgen)
        bot = new(token); //, "https://api.telegram.org/bot", "https://api.telegram.org/file/bot"
        try
        {
            //SendMessage("Started telegram bot!");

            var me = await bot.GetMeAsync();
            //GlobalData.AddTextToLogTab($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
            //return; //t'ding crasht en is niet fijn



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

            // Dat moet ook nog eens wat netter met een CT
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //SendMessage("hello over there!");
                    Update[] updates = await bot.GetUpdatesAsync(offset);

                    foreach (Update update in updates)
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
                                case MessageType.Text:
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
                                            Helper.ShowPositions(stringBuilder);
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
                                            Helper.ShowPositions(stringBuilder);
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
                                            await AssetTools.FetchAssetsAsync(GlobalData.ActiveExchange!);
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
                                                await bot.SendTextMessageAsync(update.Message.Chat.Id, s, parseMode: ParseMode.Html, disableWebPagePreview: true);
                                            else
                                                await bot.SendTextMessageAsync(update.Message.Chat.Id, s);
                                        }
                                    }
                                    break;

                                    //    case MessageType.Photo:
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
                        catch (RequestException error)
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
                catch (Exception error)
                {
                    // Stupid Telegram is not playing nice
                    //ScannerLog.Logger.Error(error, "");
                    ScannerLog.Logger.Error($"ERROR telegram thread {error.Message}"); // simplify error on 1 line
                    GlobalData.AddTextToLogTab($"ERROR telegram thread {error.Message}");
                }
                await Task.Delay(500);
            }
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ERROR telegram thread {error.Message}");
        }
        GlobalData.AddTextToLogTab("Task Telegram stopped");
    }

}
