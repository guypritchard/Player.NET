namespace DJPadTests
{
    using System;
    using System.Buffers.Binary;
    using System.Drawing;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Vis;
    using DJPad.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ArtworkVisualisationTests
    {
        [TestMethod]
        public void ArtworkEffectsRenderAudioReactiveFrames()
        {
            using var artwork = new Bitmap(100, 100);
            using (var graphics = Graphics.FromImage(artwork))
            {
                graphics.Clear(Color.CornflowerBlue);
                graphics.FillEllipse(Brushes.Gold, 20, 20, 60, 60);
            }

            var source = new SineSource();
            var palette = new ColorPalette(new[] { Color.Navy, Color.CornflowerBlue, Color.Gold });
            IVisualisation[] effects = { new PixelMosaic(), new LiquidArtwork() };
            foreach (var effect in effects)
            {
                effect.SampleSource = source;
                ((IArtworkVisualisation)effect).Artwork = artwork;
                var frame = effect.Draw(new Size(400, 400), Color.Transparent, true, TimeSpan.FromMinutes(3), palette);

                Assert.AreEqual(400, frame.Width);
                Assert.AreEqual(400, frame.Height);
                Assert.IsTrue(frame.GetPixel(200, 200).A > 0, $"{effect.GetType().Name} produced an empty frame.");
            }
        }

        private sealed class SineSource : ISampleSource
        {
            private readonly FormatInformation format = new() { BytesPerSample = 2, Channels = 2, SampleRate = 44100 };
            private int sampleOffset;

            public FormatInformation GetFormat() => format;

            public Sample GetSample(int dataRequested)
            {
                var sample = new Sample(dataRequested) { Format = format };
                for (var offset = 0; offset < dataRequested - 3; offset += 4)
                {
                    var value = (short)(Math.Sin(sampleOffset++ * 2 * Math.PI * 220 / format.SampleRate) * 12000);
                    BinaryPrimitives.WriteInt16LittleEndian(sample.Data.AsSpan(offset, 2), value);
                    BinaryPrimitives.WriteInt16LittleEndian(sample.Data.AsSpan(offset + 2, 2), value);
                }
                return sample;
            }
        }
    }
}
