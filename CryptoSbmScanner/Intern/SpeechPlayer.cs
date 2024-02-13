using System.Collections.Concurrent;
using System.Speech.Synthesis;

using Humanizer;

namespace CryptoSbmScanner.Intern;

static public class ThreadSpeechPlayer
{
    private static Thread speechPlayThread;
    private static readonly BlockingCollection<string> speechQueue = new();
    private static readonly CancellationTokenSource speechCancelToken = new();

    public static void AddToQueue(string filename)
    {
        speechQueue.Add(filename);
        StartSpeechThread();
    }


    private static void StartSpeechThread()
    {
        //Sound Player Loop Thread
        if (speechPlayThread == null || !speechPlayThread.IsAlive)
        {
            speechPlayThread = new Thread(SpeechThreadExecute)
            {
                Name = "SpeechPlayer",
                IsBackground = true
            };
            speechPlayThread.Start();
        }
    }

    /// <summary>
    /// Method that the outside thread will use outside the thread of this class
    /// </summary>
    public static void SpeechThreadExecute()
    {
        try
        {
            using SpeechSynthesizer synthesizer = new();
            // to change VoiceGender and VoiceAge check out those links below
            synthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
            synthesizer.Volume = 100;  // (0 - 100)
            synthesizer.Rate = 0;     // (-10 - 10)

            foreach (string text in speechQueue.GetConsumingEnumerable(speechCancelToken.Token))
            {
                synthesizer.Speak(text);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString(), true);
        }
    }
}
