using System.Collections.Concurrent;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Core;

namespace CryptoScanBot.Intern;

static public class ThreadSoundPlayer
{
    private static Thread? soundThread = null;
    private static readonly BlockingCollection<(string soundFile, bool test)> soundQueue = new();
    private static readonly CancellationTokenSource soundCancelToken = new();

    public static void AddToQueue(string soundFile, bool test)
    {
        soundQueue.Add((soundFile, test));
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
            foreach ((string soundFile, bool test) in soundQueue.GetConsumingEnumerable(soundCancelToken.Token))
            {
                string fileName;
                if (Path.GetDirectoryName(soundFile) != "")
                    fileName = soundFile;
                else
                    fileName = GlobalData.AppPath + @"\Sounds\" + soundFile;

                // Als we binnen x seconden hetzelfde bestand afspelen negeren we het (anders een eindeloze reeks met pingeltjes)
                if (!test && lastFile == fileName && (DateTime.Now - last).TotalSeconds < 15)
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
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}
