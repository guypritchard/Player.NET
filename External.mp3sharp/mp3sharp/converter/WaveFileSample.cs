namespace javazoom.jl.converter
{
    internal class WaveFileSample
    {
        #region Fields

        public short[] Chan;

        #endregion

        #region Constructors and Destructors

        public WaveFileSample(WaveFile enclosingInstance)
        {
            this.InitBlock(enclosingInstance);
            this.Chan = new short[WaveFile.MAX_WAVE_CHANNELS];
        }

        #endregion

        #region Public Properties

        public WaveFile EnclosingInstance { get; private set; }

        #endregion

        #region Methods

        private void InitBlock(WaveFile enclosingInstance)
        {
            this.EnclosingInstance = enclosingInstance;
        }

        #endregion
    }
}