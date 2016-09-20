namespace DJPad.Core.Utils
{
    using System;

    public static class TimeSpanExtensions
    {
        public static string Consise(this TimeSpan span)
        {
            return span.Hours >= 1
                    ? span.ToString(@"h\:mm\:ss")
                    : span.ToString(@"m\:ss");
        }

        public static string Detailed(this TimeSpan span)
        {
            return span.Hours >= 1
                    ? span.ToString(@"h\:mm\:ss")
                    : span.Minutes >= 1
                        ? span.ToString(@"m\:ss")
                        : span.ToString(@"m\:ss\.f");
        }
    }
}
