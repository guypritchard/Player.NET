namespace DJPad.Core.Interfaces
{
    public interface ISampleConsumer
    {
        #region Public Properties

        ISampleSource SampleSource { get; set; }

        #endregion
    }
}