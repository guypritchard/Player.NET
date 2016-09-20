namespace DJPad.Core
{
    using DJPad.Core;

    public class FormatInformation
    {
        #region Public Properties

        public ushort BytesPerSample { get; set; }

        public int Channels { get; set; }

        public int SampleRate { get; set; }

        #endregion

        #region Public Methods and Operators

        public WaveFormat ToWaveFormat()
        {
            return new WaveFormat(this.SampleRate, (short)(this.BytesPerSample * 8), (short)this.Channels);
        }

        #endregion

        #region Methods

        public int SamplesPerSecond
        {
            get
            {
                return this.BytesPerSample * this.SampleRate * this.Channels;
            }
        }

        #endregion
    }
}