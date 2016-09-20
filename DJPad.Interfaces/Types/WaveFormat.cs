namespace DJPad.Core
{
    public class WaveFormat
    {
        public enum WaveFormatType
        {
            Pcm = 1,
            Float = 3
        }

        public WaveFormatType Format { get; private set; }

        public short Channels { get; private set; }

        public int SamplesPerSec { get; private set; }

        public int AvgBytesPerSec { get; private set; }

        public short BlockAlignment { get; private set; }

        public short BitsPerSample { get; private set; }

        public WaveFormat(int rate, short bits, short channels)
        {
            this.Format = WaveFormatType.Pcm;
            this.Channels = channels;
            this.SamplesPerSec = rate;
            this.BitsPerSample = bits;
            this.BlockAlignment = (short)(channels * (bits / 8));
            this.AvgBytesPerSec = this.SamplesPerSec * this.BlockAlignment;
        }
    }
}
