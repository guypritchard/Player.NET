namespace DJPad
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Forms;
    using System.Linq;
    
    using DJPad.Core;
    using DJPad.Core.BeatDetector;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Mixer;
    using DJPad.Core.Vis;
    using DJPad.Db;
    using DJPad.Db.Mongo;
    using DJPad.Output.DirectSound;
    using DJPad.Output.Null;
    using DJPad.Output.Wave;
    using DJPad.Sources;
    using DJPad.Core.Utils;

    internal class Program
    {
        #region Public Methods and Operators

        public static void PlayAndMix2Mp3s(string first, string second)
        {
            var source = new Mp3Source();
            source.Load(first);
            var source2 = new Mp3Source();
            source2.Load(second);

            var m = new Mixer(128 * 1024);
            m.Sources.Add(source);
            m.Sources.Add(source2);

            var w = new WaveOut { SampleSource = m };
            w.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            w.Stop();
        }

        public static void PlayCDTrack(char CdDrive, int trackNumber)
        {
            var source = new CdSource(CdDrive, trackNumber);

            var cb = new CircularBuffer { SampleSource = source };

            var w = new WaveOut { SampleSource = cb };
            w.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            w.Stop();
        }

        #endregion

        #region Methods

        private static void GetID3Info(string mp3File)
        {
            var source = new Mp3Source();
            source.Load(mp3File);

            var ds = new DirectSoundOut { SampleSource = source };

            ds.Play();

            Console.WriteLine(source.GetMetadata().Title);
            Console.WriteLine(source.GetMetadata().Album);
            Console.WriteLine(source.GetMetadata().Artist);

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            ds.Stop();
        }

        //public static void PlayListTest()
        //{
        //    Playlist playlist =
        //        PlaylistGenerator.GenerateFromDirectory(
        //            @"..\..\..\..\..\Music\Random Access Memories [+digital booklet]");


        //    var vis = new Oscilloscope();
        //    var ds = new DirectSoundOut {  };

        //    foreach (PlaylistItem item in playlist.LoadedCurrent)
        //    {
        //        var buffer = new CircularBuffer() { SampleSource = item.Source };

        //        vis.SampleSource = buffer;
        //        vis.SetMetadata(item.Source.GetMetadata());

        //        ds.Play();

        //        var host = new VisualisationHost(vis) { DisplayFPS = true };
        //        ds.FinishedEvent += (source, args) =>
        //            {
        //                if (host.InvokeRequired)
        //                {
        //                    host.Invoke(new Action(() => host.Close()));
        //                }
        //                else
        //                {
        //                    host.Close();
        //                }
        //            };

        //        Application.Run(host);
        //        ds.Stop();
        //        item.Source.Close();
        //    }
        //}

        public static void PlayWith4Channels(string wavFile)
        {
            var source = new WaveSource();
            source.Load(wavFile);

            var w = new FourChannelOut { SampleSource = source };

            w.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            w.Stop();
        }

        public static void SeeFFT(string mp3File)
        {
            var source = new Mp3Source();
            source.Load(mp3File);                               

            var vis = new Oscilloscope { SampleSource = source };
            var ds = new DirectSoundOut {  };

            ds.Play();

            Console.WriteLine(source.GetMetadata().Title);
            Console.WriteLine(source.GetMetadata().Album);
            Console.WriteLine(source.GetMetadata().Artist);

            vis.SetMetadata(source.GetMetadata());

            var host = new VisualisationHost(vis);
            Application.Run(host);
        }

        private static void Main(string[] args)
        {
            //var source = new Mp3Source();
            //source.Load(@"..\..\..\..\..\Music\01-02- Under Control.mp3");
            //source.GetMetadata();

            //var beats = new BeatDetector { SampleSource = source };
            //var nullOut = new NullOutput { SampleSource = beats };
            //Console.WriteLine("Decoding...");
            //nullOut.PlayToEnd();
            //Console.WriteLine("Complete.");

            ScanForFilesMongo();
        }

        private static void CreateWaveForm()
        {
            var producer = new WaveformImageProducer(@"C:\Users\Jeff\Dropbox\Music\marvin gaye mixx.mp3");

            while (!producer.Complete)
            {
                producer.DrawWaveform(800, 50, 0.0d).Save(string.Format("WaveForm{0}.bmp", 1));
                Thread.Sleep(1000);
            }
        }

        private static void FastDecompress()
        {
            var source = new Mp3Source();
            var nullOut = new NullOutput { SampleSource = source };

            source.Load(@"C:\Users\Jeff\Music\Amazon MP3\Childish Gambino\Because The Internet [Explicit]\01-17- I pink toes [feat Jhené Aiko] [Explicit].mp3");
            source.GetMetadata();
            Console.WriteLine("Decoding...");
            nullOut.PlayToEnd();
            Console.WriteLine("Complete");
        }

        private static void ScanForFiles()
        {
            IMediaCorpus corpus = new SqlCeMediaCorpus();
            Scanner s = new Scanner(@"C:\Users\Jeff\Music\",
                new[] { ".mp3" },
                directory =>
                {
                    Debug.WriteLine(directory);
                    corpus.Initialize(directory, forceRecreate:true);
                },
                mp3File =>
                {
                    Debug.WriteLine(mp3File);
                    var source = new Mp3Source();
                    source.Load(mp3File);
                    var metadata = source.GetMetadata();
                    corpus.AddOrUpdate(metadata, mp3File);
                });

            Console.WriteLine("Scanning started");
            s.Scan().Wait();
            Console.WriteLine("Scanning complete");
            var albums = corpus.Albums().ToArray();
            albums.ToList().ForEach(a => Console.WriteLine(a.Name));

            Console.ReadLine();
        }

        private static void ScanForFilesMongo()
        {
            IMediaCorpus corpus = new MongoMediaCorpus();
            Scanner s = new Scanner(@"C:\Users\Jeff\Music\",
                new[] { ".mp3" },
                directory =>
                {
                    Debug.WriteLine(directory);
                    corpus.Initialize(directory, forceRecreate: true);
                },
                mp3File =>
                {
                    Debug.WriteLine(mp3File);
                    var source = new Mp3Source();
                    source.Load(mp3File);
                    var metadata = source.GetMetadata();
                    corpus.AddOrUpdate(metadata, mp3File);
                });

            Console.WriteLine("Scanning started");
            s.Scan().Wait();
            Console.WriteLine("Scanning complete");
            var albums = corpus.Albums().ToArray();
            albums.ToList().ForEach(a => Console.WriteLine(a.Name));

            Console.ReadLine();
        }

        private static void PlayAndMixTwoWaves(string wavFile, string wavFile2)
        {
            var source = new WaveSource();
            source.Load(wavFile);
            var source2 = new WaveSource();
            source2.Load(wavFile2);
            var m = new Mixer(128 * 1024);
            m.Sources.Add(source);
            m.Sources.Add(source2);

            var w = new WaveOut { SampleSource = m };
            w.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            w.Stop();
        }

        private static void PlayMp3(string mp3File)
        {
            var source = new Mp3Source();
            source.Load(mp3File);

            var w = new WaveOut { SampleSource = source };
            w.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            w.Stop();
        }

        private static void PlayMp3DirectSound(string wavFile)
        {
            var source = new Mp3Source();
            source.Load(wavFile);

            var ds = new DirectSoundOut { SampleSource = source };

            ds.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            ds.Stop();
        }

        private static void PlayWave(string wavFile)
        {
            var source = new WaveSource();
            source.Load(wavFile);
            var cb = new CircularBuffer { SampleSource = source };
            var w = new WaveOut { SampleSource = cb };

            w.Play();

            Console.WriteLine("Press any key to stop.");
            Console.ReadLine();

            w.Stop();
        }


        private static void DetectBeats()
        {

        }

        #endregion
    }
}