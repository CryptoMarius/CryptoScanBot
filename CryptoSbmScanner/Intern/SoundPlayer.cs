using System.Collections.Concurrent;

namespace CryptoSbmScanner.Intern;

static public class ThreadSoundPlayer
{
    private static Thread soundThread;
    private static readonly BlockingCollection<string> soundQueue = new();
    private static readonly CancellationTokenSource soundCancelToken = new();

    public static void AddToQueue(string filename)
    {
        soundQueue.Add(filename);
        StartSoundThread();
    }


    private static void StartSoundThread()
    {
        //Sound Player Loop Thread
        if (soundThread == null || !soundThread.IsAlive)
        {
            soundThread = new Thread(SoundThreadExecute)
            {
                Name = "SoundPlayer",
                IsBackground = true
            };
            soundThread.Start();
        }

    }

    /// <summary>
    /// Method that the outside thread will use outside the thread of this class
    /// </summary>
    private static void SoundThreadExecute()
    {
        string lastFile = "";
        DateTime last = DateTime.Now;
        try
        {
            // https://stackoverflow.com/questions/22208258/how-to-play-sounds-asynchronuously-but-themselves-in-a-queue
            using System.Media.SoundPlayer soundPlayer = new();
            foreach (string text in soundQueue.GetConsumingEnumerable(soundCancelToken.Token))
            {
                string fileName;
                if (Path.GetDirectoryName(text) != "")
                    fileName = text;
                else
                    fileName = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\Sounds\" + text;

                // Als we binnen x seconden hetzelfde bestand afspelen negeren we het (anders een eindeloze reeks met pingeltjes)
                if (lastFile == fileName && (DateTime.Now - last).TotalSeconds < 5)
                    continue;
                
                last = DateTime.Now;
                lastFile = fileName;

                if (File.Exists(fileName))
                {
                    // http://msdn.microsoft.com/en-us/library/system.media.soundplayer.playsync.aspx
                    soundPlayer.SoundLocation = fileName;
                    //Here the outside thread waits for the following play to end before continuing.
                    soundPlayer.PlaySync();
                }
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
