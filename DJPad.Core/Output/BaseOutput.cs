namespace DJPad.Output
{
    using System;
    using System.Diagnostics;
    using DJPad.Core.Interfaces;

    public delegate void BufferFillEventHandler(IntPtr data, int size);

    public abstract class BaseOutput : ISampleConsumer
    {
        #region Public Events

        public virtual event EventHandler FinishedEvent;

        public virtual event EventHandler StoppedEvent;

        #endregion

        #region Public Properties

        public ISampleSource SampleSource { get; set; }

        public long SamplesPlayed { get; set; }

        #endregion

        #region Methods

        protected void FireFinishedEvent(object obj)
        {
            Debug.WriteLine("BaseOutput: Finished Playback.");
            EventHandler onFinishedEvent = this.FinishedEvent;
            if (onFinishedEvent != null)
            {
                onFinishedEvent(this, null);
            }
        }

        protected void FireStoppedEvent(object obj)
        {
            Debug.WriteLine("BaseOutput: Playback Stopped.");
            EventHandler onStoppedEvent = this.StoppedEvent;
            if (onStoppedEvent != null)
            {
                onStoppedEvent(this, null);
            }
        }

        #endregion
    }
}