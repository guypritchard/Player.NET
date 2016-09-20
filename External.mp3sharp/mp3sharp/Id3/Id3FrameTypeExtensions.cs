namespace ID3
{
    public static class Id3FrameTypeExtensions
    {
        #region Public Methods and Operators

        public static bool IsImage(this Id3FrameType type)
        {
            return type == Id3FrameType.APIC;
        }

        public static bool IsText(this Id3FrameType type)
        {
            if (type == Id3FrameType.TIT1 || type == Id3FrameType.TIT2 || type == Id3FrameType.TIT3
                || type == Id3FrameType.TALB || type == Id3FrameType.TOAL || type == Id3FrameType.TRCK
                || type == Id3FrameType.TPOS || type == Id3FrameType.TSST || type == Id3FrameType.TSRC
                || type == Id3FrameType.TPE1 || type == Id3FrameType.TPE2 || type == Id3FrameType.TPE3
                || type == Id3FrameType.TPE4 || type == Id3FrameType.COMM || type == Id3FrameType.TCOP
                || type == Id3FrameType.TCOM || type == Id3FrameType.TYER || type == Id3FrameType.TCON 
                || type == Id3FrameType.PRIV || type == Id3FrameType.TLEN || type == Id3FrameType.TBPM
                || type == Id3FrameType.TMED)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}