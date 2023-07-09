using System.Text;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

using Dapper;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoSbmScanner.Intern;


// Talk to BotFather on Telegram
// (find the user and)
// ask him: /newbot
//
// This command will create a new bot for you
// It will ask two things:
// 1: Choose a name for your bot: <BotName>
// 2: Choose a username for your bot: <BotName>Bot
//
// Copy the Token (6105320626:AAFGrpm2gmBhD7Oi0AnM2sQjTooG-zerX2g)
// Paste it into the "Telegram Token" field of the Scanner telegram settings 

// Go the suggested Chat BotFather created
// Type ChatId (a command of the Scanner)
// Copy the ChatId (710219603)
// Paste it into the "Telegram ChatId" field of the Scanner telegram settings
// Press the test button, that should work (I hope)
//
// Lots of other commands available
//



public class ThreadTelegramBot
{
    //public Thread Thread;
    private static int offset;
    public static bool running;
    private static TelegramBotClient bot;
    private readonly CancellationTokenSource cancellationToken = new();

    //public ThreadTelegramBot()
    //{
    //    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
    //    // Extra parameters vanwege ambigious constructor (die ik niet geheel kon volgen)
    //    bot = new TelegramBotClient(BotToken, "https://api.telegram.org/bot", "https://api.telegram.org/file/bot");

    //    Thread = new(Execute)
    //    {
    //        Name = "ThreadTelegramBot",
    //        IsBackground = true
    //    };
    //}

    public void Stop()
    {
        cancellationToken.Cancel();
        GlobalData.AddTextToLogTab(string.Format("Stop telegram handler"));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    async static public void SendMessage(string text)
    {
        if (bot == null)
            return;

        try
        {
            await bot.SendTextMessageAsync(GlobalData.Settings.Telegram.ChatId, text);
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
        }
    }



    private static void StartBot(string arguments, StringBuilder stringbuilder)
    {
        bool soundSignal = false;
        bool balanceBot = false;
        bool signalsBot = false;
        bool adviceOnly = false;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
        {
            soundSignal = parameters[1].Trim().ToLower().Equals("sound");
            adviceOnly = parameters[1].Trim().ToLower().Equals("advice");
            signalsBot = parameters[1].Trim().ToLower().Equals("signals");
            balanceBot = parameters[1].Trim().ToLower().Equals("balancing");
        }

        if (soundSignal)
        {
            //if (!GlobalData.Settings.Signal.SoundSignalNotification)
            //{
            //    GlobalData.Settings.Signal.SoundSignalNotification = true;
            //    stringbuilder.AppendLine("Sound started!");
            //    GlobalData.SaveSettings();
            //}
            //else
            //    stringbuilder.AppendLine("Sound is already active!");
        }
        else if (balanceBot)
        {
            if (!GlobalData.Settings.BalanceBot.Active)
            {
                GlobalData.Settings.BalanceBot.Active = true;
                stringbuilder.AppendLine("Balance bot started!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Balance bot already active!");
        }
        else if (adviceOnly)
        {
            if (!GlobalData.Settings.BalanceBot.ShowAdviceOnly)
            {
                GlobalData.Settings.BalanceBot.ShowAdviceOnly = true;
                stringbuilder.AppendLine("Balance bot advice only started!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Balance bot advice only already active!");
        }
        else if (signalsBot)
        {
            if (!GlobalData.Settings.Signal.SignalsActive)
            {
                GlobalData.Settings.Signal.SignalsActive = true;
                stringbuilder.AppendLine("Signal bot started!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Signal bot already active!");
        }
        else
        {
            if (!GlobalData.Settings.Trading.Active)
            {
                GlobalData.Settings.Trading.Active = true;
                stringbuilder.AppendLine("Bot started!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Bot already active!");
        }
    }


    private static void StopBot(string arguments, StringBuilder stringbuilder)
    {
        bool sound = false;
        bool balanceBot = false;
        bool signalsBot = false;
        bool adviceOnly = false;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
        {
            sound = parameters[1].Trim().ToLower().Equals("sound");
            adviceOnly = parameters[1].Trim().ToLower().Equals("advice");
            signalsBot = parameters[1].Trim().ToLower().Equals("signals");
            balanceBot = parameters[1].Trim().ToLower().Equals("balancing");
        }

        if (sound)
        {
            //if (GlobalData.Settings.Signal.SoundSignalNotification)
            //{
            //    GlobalData.Settings.Signal.SoundSignalNotification = false;
            //    stringbuilder.AppendLine("Sound stopped!");
            //    GlobalData.SaveSettings();
            //}
            //else
            //    stringbuilder.AppendLine("Sound is already inactive!");
        }
        else if (balanceBot)
        {
            if (GlobalData.Settings.BalanceBot.Active)
            {
                GlobalData.Settings.BalanceBot.Active = false;
                stringbuilder.AppendLine("Balance bot stopped!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Balance bot already inactive!");
        }
        else if (adviceOnly)
        {
            if (GlobalData.Settings.BalanceBot.ShowAdviceOnly)
            {
                GlobalData.Settings.BalanceBot.ShowAdviceOnly = false;
                stringbuilder.AppendLine("Balance bot advice only stopped!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Balance bot advice only inactive!");
        }
        else if (signalsBot)
        {
            if (GlobalData.Settings.Signal.SignalsActive)
            {
                // TODO: User interface ook updaten
                GlobalData.Settings.Signal.SignalsActive = false;
                stringbuilder.AppendLine("Signal bot stopped!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Signal bot already inactive!");
        }
        else
        {
            if (GlobalData.Settings.Trading.Active)
            {
                // TODO: User interface ook updaten
                GlobalData.Settings.Trading.Active = false;
                stringbuilder.AppendLine("Bot stopped!");
                GlobalData.SaveSettings();
            }
            else
                stringbuilder.AppendLine("Bot already inactive!");
        }
    }


    private static void ShowTrend(string arguments, StringBuilder stringbuilder)
    {
        string symbolstr = "";
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
            symbolstr = parameters[1].Trim().ToUpper();
        stringbuilder.AppendLine(string.Format("Trend {0}", symbolstr));


        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolstr, out CryptoSymbol symbol))
            {
                int iterator = 0;
                long percentageSum = 0;
                long maxPercentageSum = 0;
                for (CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval1m; intervalPeriod <= CryptoIntervalPeriod.interval1d; intervalPeriod++)
                {
                    iterator++;
                    if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
                        return;

                    TrendIndicator trendIndicatorClass = new(symbol, interval);
                    CryptoTrendIndicator trendIndicator = trendIndicatorClass.CalculateTrend();

                    string s;
                    if (trendIndicator == CryptoTrendIndicator.trendBullish)
                        s = "trend=bullish";
                    else if (trendIndicator == CryptoTrendIndicator.trendBearish)
                        s = "trend=bearish";
                    else
                        s = "trend=sideway's?";
                    stringbuilder.AppendLine(string.Format("{0} {1:N2}", interval.Name, s));

                    if (trendIndicator == CryptoTrendIndicator.trendBullish)
                        percentageSum += (int)intervalPeriod * iterator;
                    else if (trendIndicator == CryptoTrendIndicator.trendBearish)
                        percentageSum -= (int)intervalPeriod * iterator;

                    // Wat is het maximale som (voor de eindberekening)
                    maxPercentageSum += (int)intervalPeriod * iterator;
                }

                decimal trendPercentage = 100 * (decimal)percentageSum / maxPercentageSum;
                if (trendPercentage < 0)
                    stringbuilder.AppendLine(string.Format("Symbol trend {0} bearish", (-trendPercentage).ToString("N2")));
                else if (trendPercentage > 0)
                    stringbuilder.AppendLine(string.Format("Symbol trend {0} bullish", trendPercentage.ToString("N2")));
                else
                    stringbuilder.AppendLine(string.Format("Symbol trend {0} unknown", trendPercentage.ToString("N2")));
            }
        }
    }

    private static void ShowBarometer(string arguments, StringBuilder stringbuilder)
    {
        //string text = "\\\Bla /Bla , Bla, hello 'h'";
        string quote = "USDT";
        string[] parameters = arguments.Split(' ');
        if (parameters.Length > 1)
            quote = parameters[1].Trim().ToUpper();

        stringbuilder.AppendLine(string.Format("Barometer {0}", quote));

        // Even een quick fix voor de barometer
        if (GlobalData.Settings.QuoteCoins.TryGetValue(quote, out CryptoQuoteData quoteData))
        {
            for (CryptoIntervalPeriod interval = CryptoIntervalPeriod.interval5m; interval <= CryptoIntervalPeriod.interval1d; interval++)
            {
                if (interval == CryptoIntervalPeriod.interval5m || interval == CryptoIntervalPeriod.interval15m || interval == CryptoIntervalPeriod.interval30m ||
                     interval == CryptoIntervalPeriod.interval1h || interval == CryptoIntervalPeriod.interval4h || interval == CryptoIntervalPeriod.interval1d)
                {
                    BarometerData barometerData = quoteData.BarometerList[(long)interval];
                    stringbuilder.AppendLine(string.Format("{0} {1:N2}", interval, barometerData.PriceBarometer));
                }
            }
        }
    }

    private static void ShowProfits(string arguments, StringBuilder stringbuilder)
    {
        decimal sumInvested = 0;
        decimal sumProfit = 0;
        decimal sumCount = 0;
        decimal percentage = 0;

        using (CryptoDatabase databaseThread = new())
        {
            databaseThread.Close();
            databaseThread.Open();

            foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from position " +
                "where CreateTime >= @fromDate and status=2", new { fromDate = DateTime.Today }))
            {
                sumCount++;
                sumProfit += position.Profit;
                sumInvested += position.Invested;
            }
            if (sumInvested > 0)
                percentage = 100 * sumProfit / sumInvested;

            stringbuilder.AppendLine(string.Format("Invested {0:N2}, profits {1:N2}, {2:N2}%", sumInvested, sumProfit, percentage));
        }
    }


    private static void ShowStatus(string arguments, StringBuilder stringbuilder)
    {
        if (arguments.Length != 1000)
            stringbuilder.AppendLine("not supported");

        //Bot status
        if (GlobalData.Settings.Trading.Active)
            stringbuilder.AppendLine("Trade bot is active!");
        else
            stringbuilder.AppendLine("Trade bot is not active!");

        // Balance bot status
        if (GlobalData.Settings.BalanceBot.Active)
            stringbuilder.AppendLine("Balance bot is active!");
        else
            stringbuilder.AppendLine("Balance bot is not active!");

        // Balance bot advice status
        if (GlobalData.Settings.BalanceBot.ShowAdviceOnly)
            stringbuilder.AppendLine("Balance bot showing advice!");


        // Create signals
        if (GlobalData.Settings.Signal.SignalsActive)
            stringbuilder.AppendLine("Signal bot is active!");
        else
            stringbuilder.AppendLine("Signal bot is not active!");

        //// Create sound
        //if (GlobalData.Settings.Signal.SoundSignalNotification)
        //    stringbuilder.AppendLine("Signal sound is active!");
        //else
        //    stringbuilder.AppendLine("Signal sound is not active!");

        // Trade sound
        if (GlobalData.Settings.General.SoundTradeNotification)
            stringbuilder.AppendLine("Trade sound is active!");
        else
            stringbuilder.AppendLine("Trade sound is not active!");
    }


    private static void ShowCoins(string arguments, StringBuilder stringbuilder)
    {
        string value;
        string[] parameters = arguments.Split(' ');
        if (parameters.Length >= 2)
            value = parameters[1];
        else
            value = "BNBUSDT,BTCUSDT,ETHUSDT,PAXGUSDT";
        parameters = value.Split(',');

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (string symbolName in parameters)
            {
                if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
                {
                    if (symbol.LastPrice.HasValue)
                    {
                        string text = string.Format("{0} waarde {1:N2}", symbolName, (decimal)symbol.LastPrice);
                        stringbuilder.AppendLine(text);
                    }
                }

            }

        }
    }


    private static void ShowHelp(StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("status        show status bots");

        stringBuilder.AppendLine("start         start trade bot");
        stringBuilder.AppendLine("stop          stop trade bot");
        stringBuilder.AppendLine("positions     show positions trade bot");
        stringBuilder.AppendLine("profits       show profits trade bot (today)");
#if BALANCING
        stringBuilder.AppendLine("balancestart  start balancing bot");
        stringBuilder.AppendLine("balancestop   stop balancing bot");
        stringBuilder.AppendLine("advicestart   start advice balancing bot");
        stringBuilder.AppendLine("advicestop    stop advice balancing bot");
        stringBuilder.AppendLine("balance       show balance overview");
#endif

        stringBuilder.AppendLine("signalstart   start signal bot");
        stringBuilder.AppendLine("signalstop    stop signal bot");

        stringBuilder.AppendLine("value         show value BTC,BNB and ETH");
        stringBuilder.AppendLine("barometer     show barometer BTC/BNB/USDT");
        stringBuilder.AppendLine("assets        show asset overview");

        stringBuilder.AppendLine("chatid        ChatId configuratie");
        stringBuilder.AppendLine("help          this stuff");
    }


    public static async Task ExecuteAsync()
    {
        return;

        // Bij het testen staat vaak de scanner aan, daatom bij sql telegram ff uit
#if !SQLDATABASE

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        //    // Extra parameters vanwege ambigious constructor (die ik niet geheel kon volgen)
        bot = new(GlobalData.Settings.Telegram.Token); //, "https://api.telegram.org/bot", "https://api.telegram.org/file/bot"
        try
        {
            //SendMessage("Started telegram bot!");

            var me = await bot.GetMeAsync();
            //GlobalData.AddTextToLogTab($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
            //return; //t'ding crasht en is niet fijn


            using CancellationTokenSource cts = new();

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
            running = true;
            while (running) //!cancellationToken.IsCancellationRequested
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
                                        StringBuilder stringBuilder = new();
                                        var arguments = update.Message.Text.ToUpper();

                                        string command = "";
                                        string[] parameters = arguments.Split(' ');
                                        if (parameters.Length > 0)
                                            command = parameters[0].Trim().ToUpper();

                                        if (command == "STATUS")
                                            ShowStatus(command, stringBuilder);
                                        else if (command == "VALUE")
                                            ShowCoins(command, stringBuilder);
                                        else if (command == "POSITIONS")
                                            Helper.ShowPositions(stringBuilder);
                                        else if (command == "PROFITS")
                                            ShowProfits(arguments, stringBuilder);

                                        else if (command == "START")
                                            StartBot(arguments, stringBuilder);
                                        else if (command == "SIGNALSTART")
                                            StartBot("command signals", stringBuilder);
                                        else if (command == "ADVICESTARTS")
                                            StopBot("command advice", stringBuilder);
                                        else if (command == "BALANCESTART")
                                            StartBot("command balancing", stringBuilder);

                                        else if (command == "STOP")
                                            StopBot(arguments, stringBuilder);
                                        else if (command == "SIGNALSTOP")
                                            StopBot("command signals", stringBuilder);
                                        else if (command == "ADVICESTOP")
                                            StopBot("command advice", stringBuilder);
                                        else if (command == "BALANCESTOP")
                                            StopBot("command balancing", stringBuilder);

                                        else if (command == "BAROMETER")
                                            ShowBarometer(arguments, stringBuilder);
                                        else if (command == "BALANCE")
                                            stringBuilder.Append(BalanceSymbolsAlgoritm.LastOverviewMessage);
                                        else if (command == "ASSETS")
                                        {
                                            //Helper.ShowAssets(stringBuilder, out decimal valueUsdt, out decimal valueBtc);
                                        }
                                        else if (command == "TREND")
                                            ShowTrend(arguments, stringBuilder);
                                        else if (command == "HELP")
                                            ShowHelp(stringBuilder);
                                        else if (command == "CHATID")
                                            stringBuilder.AppendLine("ChatId: " + update.Message.Chat.Id.ToString());
                                        else stringBuilder.Append("Not a command..");


                                        string s = stringBuilder.ToString();
                                        if (s != "")
                                            await bot.SendTextMessageAsync(update.Message.Chat.Id, s);
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
                        catch (Exception error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            GlobalData.Logger.Error(error);
                            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());

                        }

                    }
                }
                catch (Exception error)
                {
                    // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                    GlobalData.Logger.Error(error);
                    GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(2)\r\n" + error.ToString());

                }
                await Task.Delay(250);
            }
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(3)\r\n" + error.ToString());
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n TELEGRAM THREAD EXIT");
#endif
    }

}
