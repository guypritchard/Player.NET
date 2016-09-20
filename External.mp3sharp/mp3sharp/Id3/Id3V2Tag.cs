namespace ID3
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Support;

    public class Id3V2Tag
    {
        #region Constants

        public const int SizeInBytes = 7;

        private const int ID3v2_FLAG_EXPERIMENTAL = 0x20;

        private const int ID3v2_FLAG_EXTENDEDHEADER = 0x40;

        private const int ID3v2_FLAG_FOOTER = 0x10;

        private const int ID3v2_FLAG_UNSYNC = 0x80;

        #endregion

        #region Fields

        public readonly char[] version = new char[2]; // Major / Minor

        public long encodedSize;

        private char flags;

        #endregion

        #region Public Methods and Operators

        public static bool IsID3(byte[] buffer)
        {
            if ((buffer[0] == 'I') && (buffer[1] == 'D') && (buffer[2] == '3'))
            {
                return true;
            }

            return false;
        }

        public static Id3V2Tag ReadTagHeader(byte[] array)
        {
            if (array.Length != SizeInBytes)
            {
                throw new ArgumentException("Id3 array wrong size.");
            }

            var ret = new Id3V2Tag();

            Array.Copy(array, 0, ret.version, 0, 2);
            ret.flags = (char)array[2];
            ret.encodedSize = SizeInBytes;
            ret.encodedSize += array[3] << 21 | array[4] << 14 | array[5] << 7 | array[6];

            return ret;
        }

        #endregion

        #region Methods

        internal IList<Id3V2Frame> ReadTagData(byte[] id3Data)
        {
            var returnFrames = new List<Id3V2Frame>();

            int currentPosition = 0;
            while (currentPosition < id3Data.Length && id3Data.Length - currentPosition > 10)
            {
                var frame = new Id3V2Frame();

                Array.Copy(id3Data, currentPosition, frame.FrameID, 0, 4);

                currentPosition += 4;

                frame.Size = SupportClass.BigEndian.ToInt32(id3Data, currentPosition);
                
                currentPosition += 4;

                frame.Flags = BitConverter.ToInt16(id3Data, currentPosition);
                
                currentPosition += 2;
                
                // Is the frame size wrong?
                if (frame.Size > id3Data.Length - currentPosition || frame.Size < 0)
                {
                    frame.Size = id3Data.Length - currentPosition;
                }

                frame.data = new byte[frame.Size];
                    
                Array.Copy(id3Data, currentPosition, frame.data, 0, frame.Size);
                   
                currentPosition += frame.Size;
                
                if (!frame.IsValid())
                {
                    Debug.WriteLine("Invalid frame found, stop scanning frames.");
                    break;
                }

                if (frame.Type.IsImage())
                {
                    returnFrames.Add(new ImageFrame(frame));
                }
                else if (frame.Type.IsText())
                {
                    returnFrames.Add(new TextFrame(frame));
                }
                else
                {
                    // Unknown frame.
                    returnFrames.Add(frame);
                }
            }

            return returnFrames;
        }

        #endregion
    }
}