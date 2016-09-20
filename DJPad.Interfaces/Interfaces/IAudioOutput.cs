namespace DJPad.Core.Interfaces
{
    using System;
    using DJPad.Core;

    public interface IAudioOutput : ISampleConsumer, ISampleSource, IStereoVolume
    {
        #region Public Methods and Operators

        void Play();

        void Stop();

        void Flush();
        
        Status State { get; }

        event EventHandler FinishedEvent;

        event EventHandler StoppedEvent;

        #endregion
    }
}