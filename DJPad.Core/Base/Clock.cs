namespace DJPad.Core
{
    using System;

    internal static class Clock
    {
        #region Static Fields

        private static DateTime StartTime;

        #endregion

        #region Public Methods and Operators

        public static TimeSpan GetOffset()
        {
            return DateTime.UtcNow - StartTime;
        }

        public static uint GetOffsetMs()
        {
            return (uint)GetOffset().TotalMilliseconds;
        }

        public static void ResetClock()
        {
            StartClock();
        }

        public static void StartClock()
        {
            StartTime = DateTime.UtcNow;
        }

        #endregion
    }
}