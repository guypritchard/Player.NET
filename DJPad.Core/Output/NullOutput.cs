namespace DJPad.Output.Null
{
    using System;
    using DJPad.Core;
    using System.Diagnostics;

    public class NullOutput : BaseOutput
    {
        public void PlayToEnd()
        {
            var chunk = this.SampleSource.GetFormat().SamplesPerSecond*2;
            var secondsRead = 0;
            Sample sample = null;
            do
            {
                sample = this.SampleSource.GetSample(chunk);
                secondsRead += 2;
                Debug.WriteLine("seconds read:" + TimeSpan.FromSeconds(secondsRead));

            } while (sample.DataLength == chunk);
        }
    }
}
