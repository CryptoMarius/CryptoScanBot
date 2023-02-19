using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoSbmScanner
{
    static public class ThreadSoundPlayer
    {
        private static Thread soundThread;
        private static BlockingCollection<string> soundQueue = new BlockingCollection<string>();
        public static CancellationTokenSource soundCancelToken = new CancellationTokenSource();

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
                soundThread = new Thread(SoundThreadExecute);
                soundThread.Name = "SoundPlayer";
                soundThread.IsBackground = true;
                soundThread.Start();
            }
        }

        /// <summary>
        /// Method that the outside thread will use outside the thread of this class
        /// </summary>
        private static void SoundThreadExecute()
        {
            // https://stackoverflow.com/questions/22208258/how-to-play-sounds-asynchronuously-but-themselves-in-a-queue
            using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer())
            {
                foreach (string text in soundQueue.GetConsumingEnumerable(soundCancelToken.Token))
                {
                    string fileName;
                    if (System.IO.Path.GetDirectoryName(text) != "")
                        fileName = text;
                    else
                        fileName = System.IO.Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location)) + @"\Sounds\" + text;

                    if (System.IO.File.Exists(fileName))
                    {
                        // http://msdn.microsoft.com/en-us/library/system.media.soundplayer.playsync.aspx
                        soundPlayer.SoundLocation = fileName;
                        //Here the outside thread waits for the following play to end before continuing.
                        soundPlayer.PlaySync();
                    }
                }
            }
        }

    }
}
