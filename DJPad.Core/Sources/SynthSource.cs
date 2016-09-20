namespace DJPad.Sources
{
    using DJPad;
    using DJPad.Core;
    using DJPad.Core.Interfaces;

    public class SynthSource : ISampleSource
    {
        #region Public Methods and Operators

        public FormatInformation GetFormat()
        {
            return new FormatInformation { BytesPerSample = 2, Channels = 2, SampleRate = 44100 };
        }

        public Sample GetSample(int dataRequested)
        {
            return null;
        }

        #endregion
    }
}