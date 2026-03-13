using CryptoScanner.Core.Core;

using NAudio.Wave;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CryptoScanner.Core.Sounds;


public static class ThreadSoundPlayer
{
    private static Thread? soundThread = null;
    private static Dictionary<string, DateTime> FilesPlayed = [];
    private static readonly BlockingCollection<string> soundQueue = [];
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
            if (!GlobalData.Settings.Signal.SoundsActive)
                return;

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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Windows: System.Media.SoundPlayer (built-in!)
                        //var player = new System.Media.SoundPlayer(fileName);
                        //player.Play();

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

                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // macOS: afplay command
                        Process.Start("afplay", fileName);
                    }
                    else // Linux
                    {
                        // Linux: aplay command
                        Process.Start("aplay", fileName);
                    }

                }
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
            GlobalData.AddTextToLogTab("Soundplayer exit");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}
