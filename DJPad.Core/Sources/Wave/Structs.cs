namespace DJPad.Formats.Wave
{
    public enum ChunkType
    {
        Riff,

        Fmt,

        Fact,

        Data,

        Unknown
    };

    /// <summary>
    ///     A class for loading into memory and manipulating wave file data.
    /// </summary>
    public class RiffChunk
    {
        #region Fields

        public string FileName;

        //These three fields constitute the riff header

        public uint dwFileLength; //In bytes, measured from offset 8

        public string sGroupID; //RIFF

        public string sRiffType; //WAVE, usually

        #endregion
    }

    public class FmtChunk
    {
        #region Fields

        public uint dwAvgBytesPerSec; //For estimating RAM allocation

        public ushort dwBitsPerSample; //Bits per sample

        public uint dwChunkSize; //Length of header

        public uint dwSamplesPerSec; //In Hz

        public string sChunkID; //Four bytes: "fmt "

        public ushort wBlockAlign; //Sample frame size in bytes

        public ushort wChannels; //Number of channels: 1-5

        public ushort wFormatTag; //1 if uncompressed

        #endregion

        /* More data can be contained in this chunk; specifically
         * the compression format data.  See MS website for this.
         */
    }

    /* The fact chunk is used for specifying the compression
       ratio of the data */

    public class FactChunk
    {
        #region Fields

        public uint dwChunkSize; //Length of header

        public uint dwNumSamples; //Number of audio frames;

        public string sChunkID; //Four bytes: "fact"

        #endregion

        //numsamples/samplerate should equal file length in seconds.
    }

    public class DataChunk
    {
        #region Fields

        public double dSecLength; //Length of audio in seconds

        public uint dwChunkSize; //Length of header

        //The following non-standard fields were created to simplify
        //editing.  We need to know, for filestream seeking purposes,
        //the beginning file position of the data chunk.  It's useful to
        //hold the number of samples in the data chunk itself.  Finally,
        //the minute and second length of the file are useful to output
        //to XML.

        public uint dwMinLength; //Length of audio in minutes

        public uint dwNumSamples; //Number of audio frames

        public long lFilePosition; //Position of data chunk in file

        public string sChunkID; //Four bytes: "data"

        #endregion

        //Different arrays for the different frame sizes
        //public byte  [] byteArray; 	//8 bit - unsigned
        //public short [] shortArray;    //16 bit - signed
    }
}