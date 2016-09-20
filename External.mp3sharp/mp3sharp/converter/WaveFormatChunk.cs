namespace javazoom.jl.converter
{
    internal class WaveFormatChunk
    {
        #region Fields

        public WaveFormatChunkData data;

        public RiffChunkHeader header;

        #endregion

        #region Constructors and Destructors

        public WaveFormatChunk(WaveFile enclosingInstance)
        {
            this.InitBlock(enclosingInstance);
            this.header = new RiffChunkHeader(enclosingInstance);
            this.data = new WaveFormatChunkData(enclosingInstance);
            this.header.ckID = RiffFile.FourCC("fmt ");
            this.header.ckSize = 16;
        }

        #endregion

        #region Public Properties

        public WaveFile EnclosingInstance { get; private set; }

        #endregion

        #region Public Methods and Operators

        public bool VerifyValidity()
        {
            return this.header.ckID == RiffFile.FourCC("fmt ") && (this.data.Channels == 1 || this.data.Channels == 2)
                   && this.data.AvgBytesPerSec
                      == (this.data.Channels * this.data.SamplesPerSec * this.data.BitsPerSample) / 8
                   && this.data.BlockAlign == (this.data.Channels * this.data.BitsPerSample) / 8;
        }

        #endregion

        #region Methods

        private void InitBlock(WaveFile enclosingInstance)
        {
            this.EnclosingInstance = enclosingInstance;
        }

        #endregion
    }
}