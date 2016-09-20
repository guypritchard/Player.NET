namespace javazoom.jl.converter
{
    internal class RiffChunkHeader
    {
        #region Fields

        public int ckID = 0; // Four-character chunk ID

        public int ckSize = 0;

        #endregion

        #region Constructors and Destructors

        public RiffChunkHeader(RiffFile enclosingInstance)
        {
            this.InitBlock(enclosingInstance);
        }

        #endregion

        #region Public Properties

        public RiffFile EnclosingInstance { get; private set; }

        #endregion

        #region Methods

        private void InitBlock(RiffFile enclosingInstance)
        {
            this.EnclosingInstance = enclosingInstance;
        }

        #endregion

        // Length of data in chunk
    }
}