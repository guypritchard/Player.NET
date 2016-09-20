namespace javazoom.jl.converter
{
    internal class WaveFormatChunkData
    {
        #region Fields

        public int AvgBytesPerSec;

        public short BitsPerSample;

        public short BlockAlign;

        public short Channels; // Number of channels (mono=1, stereo=2)

        public short FormatTag; // Format category (PCM=1)

        public int SamplesPerSec; // Sampling rate [Hz]

        #endregion

        #region Constructors and Destructors

        public WaveFormatChunkData(WaveFile enclosingInstance)
        {
            this.InitBlock(enclosingInstance);
            this.FormatTag = 1; // PCM
            this.Config(44100, 16, 1);
        }

        #endregion

        #region Public Properties

        public WaveFile EnclosingInstance { get; private set; }

        #endregion

        #region Public Methods and Operators

        public void Config(int newSamplingRate, short newBitsPerSample, short newNumChannels)
        {
            this.SamplesPerSec = newSamplingRate;
            this.Channels = newNumChannels;
            this.BitsPerSample = newBitsPerSample;
            this.AvgBytesPerSec = (this.Channels * this.SamplesPerSec * this.BitsPerSample) / 8;
            this.BlockAlign = (short)((this.Channels * this.BitsPerSample) / 8);
        }

        #endregion

        #region Methods

        private void InitBlock(WaveFile enclosingInstance)
        {
            this.EnclosingInstance = enclosingInstance;
        }

        #endregion
    }
}