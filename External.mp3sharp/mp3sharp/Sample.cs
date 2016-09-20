namespace Mp3Sharp
{
    using System;

    /// <summary>
    ///     Some samples that show the use of the Mp3Stream class.
    /// </summary>
    internal class Sample
    {
        #region Static Fields

        public static readonly string Mp3FilePath = @"c:\sample.mp3";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Sample showing how to read through an MP3 file and obtain its contents as a PCM byte stream.
        /// </summary>
        public static void ReadAllTheWayThroughMp3File()
        {
            var stream = new Mp3Stream(Mp3FilePath);

            // Create the buffer
            int numberOfPcmBytesToReadPerChunk = 512;
            var buffer = new byte[numberOfPcmBytesToReadPerChunk];

            int bytesReturned = -1;
            int totalBytes = 0;
            while (bytesReturned != 0)
            {
                bytesReturned = stream.Read(buffer, 0, buffer.Length);
                totalBytes += bytesReturned;
            }
            Console.WriteLine("Read a total of " + totalBytes + " bytes.");
        }

        #endregion
    }
}