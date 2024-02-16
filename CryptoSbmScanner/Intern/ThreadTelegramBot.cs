using System.Text;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

using Dapper;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
// Copy the Token (4105020626:AAFGrpm2gmBhX7Oi5AnM2sQjTooG-zerX2g)
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
    public static string Token { get; set; }
    public static string ChatId { get; set; }
    private static ThreadTelegramBotInstance bot;


    public static async Task Start(string token, string chatId)
    {
        // herstart?
        if (bot != null)
            Stop();

        GlobalData.AddTextToLogTab(string.Format("Start telegram handler"));
        Token = token;
        ChatId = chatId;

        bot = new();
        await bot.ExecuteAsync(token);
    }


    public static void Stop()
    {
        if (bot != null)
        {
            GlobalData.AddTextToLogTab(string.Format("Stop telegram handler"));
            bot.Stop();
        }
    }


    public static async void SendMessage(string text)
    {
        if (bot == null || text == "" || ChatId == "")
            return;
        await bot.SendMessage(text);
    }


    async static public void SendSignal(CryptoSignal signal)
    {
        if (bot == null || signal == null || ThreadTelegramBot.ChatId == "")
            return;
        await bot.SendSignal(signal);
    }
}



public class ThreadTelegramBotInstance
{
    private static int offset;
    private TelegramBotClient bot;
    public CancellationTokenSource cancellationToken = new();

    //public static string Token { get; set; }
    //public static string ChatId { get; set; }


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
            await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, text, parseMode: ParseMode.Html, disableWebPagePreview: true);
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
        }
    }


    private static string GetEmoiFromMarketTrend(float trend)
    {
        // https://beta.emojipedia.org/police-car-light

        // Circles
        if (trend > 0)
            return "\U00002B06"; // Arrow up
        else
        if (trend < 0)
            return "\U00002B07"; // Arrown down
        else
            return "\U00002753"; // questionmark
    }

    private static string GetEmoiFromTrend(CryptoTrendIndicator? trend)
    {
        // https://beta.emojipedia.org/police-car-light

        if (GlobalData.Settings.Telegram.UseEmojiInTrend)
        {
            // Circles
            return trend switch
            {
                CryptoTrendIndicator.Bullish => "\U0001f7e2",
                CryptoTrendIndicator.Bearish => "\U0001F534",
                _ => "\U000026AB",
            };
        }
        else
        {
            return trend switch
            {
                CryptoTrendIndicator.Bullish => "bull@",
                CryptoTrendIndicator.Bearish => "bear@",
                _ => "?",
            };
        }


        // Arrows
        //return trend switch
        //{
        //    CryptoTrendIndicator.Bullish => "\U00002B06",
        //    CryptoTrendIndicator.Bearish => "\U00002B07",
        //    _ => "\U00002753", // questionmark
        //};
    }

    public async Task SendSignal(CryptoSignal signal)
    {
        if (bot == null || signal == null || ThreadTelegramBot.ChatId == "")
            return;

        try
        {
            StringBuilder builder = new();
            builder.Append(signal.Symbol.Name + " " + signal.Interval.Name + " ");
            builder.Append(CandleTools.GetUnixDate(signal.Candle.OpenTime).ToLocalTime().ToString("dd MMM HH:mm"));
            builder.Append(" " + signal.StrategyText + "");
            //builder.Append(" " + signal.SideText + " ");

            // https://apps.timwhitlock.info/emoji/tables/unicode
            if (signal.Side == CryptoTradeSide.Long)
                builder.Append($"\U0001f7e2 {signal.SideText}");
            else
                builder.Append($"\U0001F534 {signal.SideText}");

            string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, signal.Exchange.Name);
            (string Url, CryptoExternalUrlType Execute) refInfo = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, true, signal.Symbol, signal.Interval);
            if (refInfo.Url != "")
                builder.Append($" <a href='{refInfo.Url}'>{text}</a>");
            builder.AppendLine();

            builder.Append("Candle: open " + signal.Candle.Open.ToString0());
            builder.Append(" high " + signal.Candle.High.ToString0());
            builder.Append(" low " + signal.Candle.Low.ToString0());
            builder.Append(" close " + signal.Candle.Close.ToString0());
            builder.AppendLine();

            builder.Append("Volume 24h: " + signal.Symbol.Volume.ToString("N0"));
            if (signal.CandlesWithZeroVolume > 0)
                builder.Append(", candles with volume " + signal.CandlesWithZeroVolume.ToString());
            builder.AppendLine();


            // De trend informatie
            // Even in de juiste volgorde toevoegen (je verwacht een vaste volgorde)
            SortedList<CryptoIntervalPeriod, (string, CryptoTrendIndicator)> a = [];
            a.TryAdd(signal.Interval.IntervalPeriod, (signal.Interval.Name, signal.TrendIndicator));
            a.TryAdd(CryptoIntervalPeriod.interval15m, ("15m", signal.Trend15m.Value));
            a.TryAdd(CryptoIntervalPeriod.interval30m, ("30m", signal.Trend30m.Value));
            a.TryAdd(CryptoIntervalPeriod.interval1h,("1h", signal.Trend1h.Value));
            a.TryAdd(CryptoIntervalPeriod.interval4h,("4h", signal.Trend4h.Value));
            a.TryAdd(CryptoIntervalPeriod.interval12h, ("12h", signal.Trend12h.Value));

            builder.Append("Trend: ");
            builder.Append(GetEmoiFromMarketTrend(signal.TrendPercentage));
            builder.Append(signal.TrendPercentage.ToString("N2") + "%");

            foreach (KeyValuePair<CryptoIntervalPeriod, (string name, CryptoTrendIndicator trendIndicator)> entry in a)
            {
                builder.Append(' ');
                builder.Append(GetEmoiFromTrend(entry.Value.trendIndicator));
                builder.Append(entry.Value.name);
            }
            builder.AppendLine();

            builder.Append("Stoch: " + signal.StochOscillator?.ToString("N2"));
            builder.Append(" Signal " + signal.StochSignal?.ToString("N2"));
            builder.Append(" RSI " + signal.Rsi?.ToString("N2"));
            builder.AppendLine();

            builder.Append("BB: " + signal.BollingerBandsPercentage?.ToString("N2") + "%");
            builder.Append(" low " + signal.BollingerBandsLowerBand?.ToString("N6"));
            builder.Append(" high " + signal.BollingerBandsUpperBand?.ToString("N6"));
            builder.AppendLine();


            //builder.Append("<b>Google</b>");
            //builder.Append("<a color:red;>Google</a>");
            //builder.Append("<span color:red;>Google</span>");

            //bot.send_message(chat_id = update.message.chat_id, text = "<a href='https://www.google.com/'>Google</a>", parse_mode = ParseMode.HTML)
            //bot.send_message(chat_id = update.message.chat_id, text = "<b>Bold font</b>", parse_mode = ParseMode.HTML)

            await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, builder.ToString(), parseMode: ParseMode.Html, disableWebPagePreview: true);

        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
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
        // TODO duplicaat code, zie de trendberekening in de commands.cs!

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
                foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.IntervalPeriodList)
                {
                    iterator++;
                    TrendIndicator trendIndicatorClass = new(symbol, cryptoSymbolInterval);
                    // TODO Parameter voor de trendIndicatorClass.CalculateTrend goed invullen
                    CryptoTrendIndicator trendIndicator = trendIndicatorClass.CalculateTrend(0);

                    string s;
                    if (trendIndicator == CryptoTrendIndicator.Bullish)
                        s = "trend=bullish";
                    else if (trendIndicator == CryptoTrendIndicator.Bearish)
                        s = "trend=bearish";
                    else
                        s = "trend=sideway's?";
                    stringbuilder.AppendLine(string.Format("{0} {1:N2}", cryptoSymbolInterval.Interval.Name, s));

                    if (trendIndicator == CryptoTrendIndicator.Bullish)
                        percentageSum += (int)cryptoSymbolInterval.IntervalPeriod * iterator;
                    else if (trendIndicator == CryptoTrendIndicator.Bearish)
                        percentageSum -= (int)cryptoSymbolInterval.IntervalPeriod * iterator;

                    // Wat is het maximale som (voor de eindberekening)
                    maxPercentageSum += (int)cryptoSymbolInterval.IntervalPeriod * iterator;
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
                    BarometerData barometerData = quoteData.BarometerList[interval];
                    stringbuilder.AppendLine(string.Format("{0} {1:N2}", interval, barometerData.PriceBarometer));
                }
            }
        }
    }

#if TRADEBOT
    private static void ShowProfits(StringBuilder stringbuilder)
    {
        decimal sumInvested = 0;
        decimal sumProfit = 0;
        decimal sumPositions = 0;

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from position " +
            "where CloseTime >= @fromDate and status=2", new { fromDate = DateTime.Today.ToUniversalTime() }))
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
#endif

    private static void ShowStatus(string arguments, StringBuilder stringbuilder)
    {
        if (arguments.Length != 1000)
            stringbuilder.AppendLine("not supported");

        //Bot status
        if (GlobalData.Settings.Trading.Active)
            stringbuilder.AppendLine($"Trade bot is active! (slots long={GlobalData.Settings.Trading.SlotsMaximalLong}, slots short={GlobalData.Settings.Trading.SlotsMaximalShort})");
        else
            stringbuilder.AppendLine("Trade bot is not active!");

#if BALANCING
        // Balance bot status
        if (GlobalData.Settings.BalanceBot.Active)
            stringbuilder.AppendLine("Balance bot is active!");
        else
            stringbuilder.AppendLine("Balance bot is not active!");

        // Balance bot advice status
        if (GlobalData.Settings.BalanceBot.ShowAdviceOnly)
            stringbuilder.AppendLine("Balance bot showing advice!");
#endif

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
        //string value;
        //string[] parameters = arguments.Split(' ');
        //if (parameters.Length >= 2)
        //    value = parameters[1];
        //else
        //    value = GlobalData.Settings.ShowSymbolInformation.ToList();
        //        //"BTCUSDT,ETHUSDT,PAXGUSDT,BNBUSDT";
        //parameters = value.Split(',');
        var parameters = GlobalData.Settings.ShowSymbolInformation;

        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (string symbolName in parameters)
            {
                if (exchange.SymbolListName.TryGetValue(symbolName + "USDT", out CryptoSymbol symbol))
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

#if TRADEBOT
        stringBuilder.AppendLine("start         start trade bot");
        stringBuilder.AppendLine("stop          stop trade bot");
        stringBuilder.AppendLine("positions     show positions trade bot");
        stringBuilder.AppendLine("profits       show profits trade bot (today)");
#endif
#if BALANCING
        stringBuilder.AppendLine("balancestart  start balancing bot");
        stringBuilder.AppendLine("balancestop   stop balancing bot");
        stringBuilder.AppendLine("advicestart   start advice balancing bot");
        stringBuilder.AppendLine("advicestop    stop advice balancing bot");
        stringBuilder.AppendLine("balance       show balance overview");
#endif

        stringBuilder.AppendLine("signalstart   start signal bot");
        stringBuilder.AppendLine("signalstop    stop signal bot");

        stringBuilder.AppendLine("value         show value BTC,BNB and ETH"); // todo, de juiste basismunten tonen
        stringBuilder.AppendLine("barometer     show barometer BTC/ETH/USDT"); // todo, de juiste basismunten tonen
#if TRADEBOT
        stringBuilder.AppendLine("assets        show asset overview");
#endif
        stringBuilder.AppendLine("chatid        ChatId configuratie");
        stringBuilder.AppendLine("help          this help screen");
    }


    public async Task ExecuteAsync(string token)
    {
        if (token == "")
            return;

            // Bij het testen staat vaak de scanner aan, daatom bij sql telegram ff uit
#if !SQLDATABASE

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
                                        StringBuilder stringBuilder = new();
                                        var arguments = update.Message.Text.ToUpper();

                                        string command = "";
                                        string[] parameters = arguments.Split(' ');
                                        if (parameters.Length > 0)
                                            command = parameters[0].Trim().ToUpper();

#if TRADEBOT
                                        if (command == ".")
                                        {
                                            ShowBarometer(arguments, stringBuilder);
                                            stringBuilder.AppendLine();
                                            Helper.ShowPositions(stringBuilder);
                                            stringBuilder.AppendLine();
                                            ShowProfits(stringBuilder);
                                            stringBuilder.AppendLine();
                                            Helper.ShowAssets(GlobalData.ExchangeRealTradeAccount, stringBuilder, out decimal _, out decimal _);
                                            stringBuilder.AppendLine();
                                            ShowCoins(command, stringBuilder);
                                        }
                                        else
#endif
                                        if (command == "STATUS")
                                            ShowStatus(command, stringBuilder);
                                        else if (command == "VALUE")
                                            ShowCoins(command, stringBuilder);
#if TRADEBOT
                                        else if (command == "POSITIONS")
                                            Helper.ShowPositions(stringBuilder);
                                        else if (command == "PROFITS")
                                            ShowProfits(stringBuilder);
#endif
                                        else if (command == "START")
                                            StartBot(arguments, stringBuilder);
                                        else if (command == "SIGNALSTART")
                                            StartBot("command signals", stringBuilder);
#if TRADEBOT
                                        else if (command == "ADVICESTARTS")
                                            StopBot("command advice", stringBuilder);
                                        else if (command == "BALANCESTART")
                                            StartBot("command balancing", stringBuilder);
#endif
                                        else if (command == "STOP")
                                            StopBot(arguments, stringBuilder);
                                        else if (command == "SIGNALSTOP")
                                            StopBot("command signals", stringBuilder);
#if TRADEBOT
                                        else if (command == "ADVICESTOP")
                                            StopBot("command advice", stringBuilder);
                                        else if (command == "BALANCESTOP")
                                            StopBot("command balancing", stringBuilder);
#endif
                                        else if (command == "BAROMETER")
                                            ShowBarometer(arguments, stringBuilder);
#if BALANCING
                                        else if (command == "BALANCE")
                                            stringBuilder.Append(BalanceSymbolsAlgoritm.LastOverviewMessage);
#endif
#if TRADEBOT
                                        else if (command == "ASSETS")
                                        {
                                            Helper.ShowAssets(GlobalData.ExchangeRealTradeAccount, stringBuilder, out decimal _, out decimal _);
                                        }
#endif
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
                        catch (HttpRequestException error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            ScannerLog.Logger.Error(error.Message);
                            //GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
                            await Task.Delay(5000);
                        }
                        catch (RequestException error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            ScannerLog.Logger.Error(error.Message);
                            //GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
                            await Task.Delay(5000);
                        }
                        catch (Exception error)
                        {
                            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                            ScannerLog.Logger.Error(error, "");
                            //GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
                            await Task.Delay(2500);
                        }

                    }
                }
                catch (Exception error)
                {
                    // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab($"ERROR telegram thread {error.Message}");
                }
                await Task.Delay(250);
            }
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ERROR telegram thread {error.Message}");
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n TELEGRAM THREAD EXIT " + token);
#endif
                                    }

                            }
