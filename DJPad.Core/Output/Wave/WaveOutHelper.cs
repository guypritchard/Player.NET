namespace DJPad.Output.Wave
{
    using System;

    using DJPad.Lib;

    internal class WaveOutHelper
    {
        #region Public Methods and Operators

        public static void Try(int err)
        {
            if (err != WaveNative.MMSYSERR_NOERROR)
            {
                throw new Exception(err.ToString());
            }
        }

        #endregion
    }
}