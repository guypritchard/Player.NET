namespace DJPad.Core.Interfaces
{
    using DJPad.Core;

    public interface ISampleSource
    {
        #region Public Methods and Operators

        FormatInformation GetFormat();

        Sample GetSample(int dataRequested);

        #endregion
    }
}