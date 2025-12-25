using CryptoScanner.Core.Core;

using NAudio.Wave;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Sounds;

public static class ThreadSoundPlayer
{
    private static Thread? soundThread = null;
    private static Dictionary<string, DateTime> FilesPlayed = [];
    private static readonly BlockingCollection<string> soundQueue = new();
    private static readonly CancellationTokenSource soundCancelToken = new();


    public static void AddToQueue(string soundFile, bool test)
    {
        string fileName;
        if (Path.GetDirectoryName(soundFile) != "")
            fileName = soundFile;
        else
            fileName = Path.Combine(GlobalData.AppPath, "Sounds", soundFile);

        // Als we binnen x seconden hetzelfde bestand afspelen negeren we het (anders een eindeloze reeks met pingeltjes)

        if (test)
        {
            // Alway's play test sounds
            soundQueue.Add(fileName);
            StartSoundThread();
        }
        else
        {
            // Ignore recently played sounds
            DateTime now = DateTime.Now;
            bool isPlayedRecently = false;
            if (FilesPlayed.TryGetValue(fileName, out DateTime last))
            {
                isPlayedRecently = (now - last).TotalSeconds < 15;
                FilesPlayed[fileName] = now;
            }
            else
                FilesPlayed.TryAdd(fileName, now);

            if (!isPlayedRecently)
            {
                soundQueue.Add(fileName);
                StartSoundThread();
            }
        }
    }


    private static void StartSoundThread()
    {
        // Sound Player Loop Thread
        if (soundThread == null || !soundThread.IsAlive)
        {
            soundThread = new Thread(() => SoundThreadExecuteAsync().GetAwaiter().GetResult())
            {
                Name = "SoundPlayer",
                IsBackground = true
            };
            soundThread.Start();
        }

    }

    public static void StopSoundThread()
    {
        try
        {
            soundCancelToken.Cancel();
            soundQueue.CompleteAdding();
            soundThread?.Join(2000); // Wait for the thread to finish
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }


    /// <summary>
    /// Method that the outside thread will use outside the thread of this class
    /// </summary>
    private static async Task SoundThreadExecuteAsync()
    {
        try
        {
            foreach (string fileName in soundQueue.GetConsumingEnumerable(soundCancelToken.Token))
            {

                if (!File.Exists(fileName))
                {
                    System.Diagnostics.Debug.WriteLine($"Sound file not found: {fileName}");
                }
                else 
                {
                    // Use NAudio for cross-platform audio playback
                    using var reader = new AudioFileReader(fileName);
                    using var output = new WaveOutEvent();

                    output.Init(reader);
                    output.Play();

                    // Wait for playback to finish
                    while (output.PlaybackState == PlaybackState.Playing && !soundCancelToken.IsCancellationRequested)
                    {
                        await Task.Delay(100);
                    }

                    //// http://msdn.microsoft.com/en-us/library/system.media.soundplayer.playsync.aspx
                    //soundPlayer.SoundLocation = fileName;
                    ////Here the outside thread waits for the following play to end before continuing.
                    //soundPlayer.PlaySync();
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
