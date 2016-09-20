namespace ID3
{
    using System;
    using System.Text;

    /// <summary>
    ///     http://id3.org/id3v2.4.0-frames
    /// </summary>
    public enum Id3FrameType
    {
        Unknown,

        APIC,

        TIT1,

        TIT2,

        TIT3,

        TALB,

        TOAL,

        TRCK,

        TPOS,

        TSST,

        TSRC,

        TPE1,

        TPE2,

        TPE3,

        TPE4,

        TLEN,

        TCON,

        TCOM,

        TCOP,

        TYER,

        COMM,

        RVAD,

        TBPM,

        GEOB, 

        TMED,

        PCNT,

        TFLT,

        UFID, 

        TENC,

        TCMP,

        TDAT,

        // Non-standard frames.
        PRIV,

        TPUB,
    }

    public class Id3V2Frame
    {
        #region Fields

        public short Flags;

        public char[] FrameID = new char[4];

        public int Size;

        public byte[] data;

        #endregion

        #region Constructors and Destructors

        public Id3V2Frame()
        {
        }

        public Id3V2Frame(Id3V2Frame frame)
        {
            this.data = frame.data;
            this.Size = frame.Size;
            this.Flags = frame.Flags;
            this.FrameID = frame.FrameID;
        }

        #endregion

        #region Public Properties

        public string Frame
        {
            get
            {
                return new string(this.FrameID);
            }
        }

        public Id3FrameType Type
        {
            get
            {
                Id3FrameType result = Id3FrameType.Unknown;
                Enum.TryParse(this.Frame, true, out result);
                return result;
            }
        }

        #endregion

        #region Public Methods and Operators

        public virtual bool IsValid()
        {
            return this.FrameID[0] != 0 || this.FrameID[1] != 0 || this.FrameID[2] != 0 || this.FrameID[3] != 0;
        }

        /// <summary>
        /// 00 – ISO-8859-1 (ASCII).
        /// 01 – UCS-2 (UTF-16 encoded Unicode with BOM), in ID3v2.2 and ID3v2.3.
        /// 02 – UTF-16BE encoded Unicode without BOM, in ID3v2.4.
        /// 03 – UTF-8 encoded Unicode, in ID3v2.4.
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="start"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public int TryReadString(int encoding, int start, out string returnValue)
        {
            returnValue = string.Empty;

            switch (encoding)
            {
                case 0:
                    return ExtractAsciiString(start, ref returnValue);
                case 1:

                    if (this.CheckForBOM(this.data, start))
                    {
                        // Skip over BOM if found.
                        start += 2;
                        returnValue = Encoding.Unicode.GetString(this.data, start, this.data.Length - start);
                        
                        // This string could be weird...  
                        // It could be a buffer part of which is actually a string, we need to look for a null terminator now we've read the data.
                        // Now resize the string and ensure we keep our index.
                        // I've only seen one Mp3 do this, and it failed Id3 validation, but plays everywhere else.  Who am I to argue?
                        var indexOf = returnValue.IndexOf('\0');
                        if (indexOf != -1 && indexOf != returnValue.Length - 1)
                        {
                            returnValue = returnValue.Substring(0, indexOf + 1);
                        }

                        return start + (returnValue.Length * 2);
                    }
                    
                    // The text encoding isn't correct, this looks like a non-Unicode string.  Falling back.
                    return ExtractAsciiString(start, ref returnValue);

                default:
                    return -1;
            }
        }

        // Check for a Byte Order Mark 0xFFFE?
        private bool CheckForBOM(byte[] rawData, int start)
        {
            return rawData[start] == 0xFF && rawData[start + 1] == 0xFE;
        }

        private int ExtractAsciiString(int start, ref string returnValue)
        {
            try
            {
                int stringend = Array.FindIndex(this.data, start, (b) => b == 0x00);
                if (stringend > -1)
                {
                    returnValue = Encoding.GetEncoding(1252).GetString(this.data, start, stringend - 1);
                    return stringend + 1;
                }

                // This is strange, but if we don't find an EOS '\0' assume the entire buffer is the string.
                returnValue = Encoding.GetEncoding(1252).GetString(this.data, start, this.data.Length - 1);
                return this.data.Length - 1;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // this sucks - I need to think about detecting invalid tags a bit more deeply.
            }

            return -1;
        }

        #endregion
    }
}