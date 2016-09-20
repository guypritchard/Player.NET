namespace DJPad.Core.BeatDetector
{
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Lib.Beatroot;

    public class BeatDetector : ISampleSource, ISampleConsumer
    {
        private ISampleSource upstream;
        private readonly AudioProcessor detector;

        public BeatDetector()
        {
            this.detector = new AudioProcessor();
        }

        public FormatInformation GetFormat()
        {
            if (this.SampleSource != null)
            {
                return this.SampleSource.GetFormat();
            }

            return new Sample().Format;
        }

        public Sample GetSample(int dataRequested)
        {
            if (this.SampleSource != null)
            {
                if (!this.detector.processFrame())
                {
                    return new Sample();
                }
            }

            return new Sample(dataRequested);
        }

        public ISampleSource SampleSource
        {
            get { return this.upstream; }
            set
            {
                this.detector.setInputFile(value);
                this.upstream = value;
            }
        }
    }
}
