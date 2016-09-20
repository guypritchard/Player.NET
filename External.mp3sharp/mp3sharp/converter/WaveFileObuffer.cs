/*
* 12/12/99	 0.0.7 Renamed class, additional constructor arguments 
*			 and larger write buffers. mdm@techie.com.
*
* 15/02/99 ,Java Conversion by E.B ,ebsp@iname.com, JavaLayer
*/

namespace javazoom.jl.converter
{
    using System;
    using System.IO;

    using javazoom.jl.decoder;

    /// <summary>
    ///     Implements an Obuffer by writing the data to
    ///     a file in RIFF WAVE format.
    ///     @since 0.0
    /// </summary>
    internal class WaveFileObuffer : Obuffer
    {
        #region Fields

        private readonly short[] buffer;

        private readonly short[] bufferp;

        private readonly int channels;

        private readonly WaveFile outWave;

        /// <summary>
        ///     Write the samples to the file (Random Acces).
        /// </summary>
        //UPGRADE_NOTE: The initialization of  'myBuffer' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
        internal short[] myBuffer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Creates a new WareFileObuffer instance.
        /// </summary>
        /// <param name="">
        ///     number_of_channels
        ///     The number of channels of audio data
        ///     this buffer will receive.
        /// </param>
        /// <param name="freq	The">
        ///     sample frequency of the samples in the buffer.
        /// </param>
        /// <param name="fileName	The">
        ///     filename to write the data to.
        /// </param>
        public WaveFileObuffer(int number_of_channels, int freq, String FileName)
        {
            this.InitBlock();
            if (FileName == null)
            {
                throw new NullReferenceException("FileName");
            }

            this.buffer = new short[OBUFFERSIZE];
            this.bufferp = new short[MAXCHANNELS];
            this.channels = number_of_channels;

            for (int i = 0; i < number_of_channels; ++i)
            {
                this.bufferp[i] = (short)i;
            }

            this.outWave = new WaveFile();

            int rc = this.outWave.OpenForWrite(FileName, null, freq, 16, (short)this.channels);
        }

        public WaveFileObuffer(int number_of_channels, int freq, Stream stream)
        {
            this.InitBlock();

            this.buffer = new short[OBUFFERSIZE];
            this.bufferp = new short[MAXCHANNELS];
            this.channels = number_of_channels;

            for (int i = 0; i < number_of_channels; ++i)
            {
                this.bufferp[i] = (short)i;
            }

            this.outWave = new WaveFile();

            int rc = this.outWave.OpenForWrite(null, stream, freq, 16, (short)this.channels);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public override void Append(int channel, short value_Renamed)
        {
            this.buffer[this.bufferp[channel]] = value_Renamed;
            this.bufferp[channel] = (short)(this.bufferp[channel] + this.channels);
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void ClearBuffer()
        {
        }

        public void close(bool justWriteLengthBytes)
        {
            this.outWave.Close(justWriteLengthBytes);
        }

        public override void Close()
        {
            this.outWave.Close();
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void SetStopFlag()
        {
        }

        public override void WriteBuffer(int val)
        {
            int k = 0;
            int rc = 0;

            rc = this.outWave.WriteData(this.buffer, this.bufferp[0]);
            // REVIEW: handle RiffFile errors. 
            /*
			for (int j=0;j<bufferp[0];j=j+2)
			{
			
			//myBuffer[0] = (short)(((buffer[j]>>8)&0x000000FF) | ((buffer[j]<<8)&0x0000FF00));
			//myBuffer[1] = (short) (((buffer[j+1]>>8)&0x000000FF) | ((buffer[j+1]<<8)&0x0000FF00));
			myBuffer[0] = buffer[j];
			myBuffer[1] = buffer[j+1];
			rc = outWave.WriteData (myBuffer,2);
			}
			*/
            for (int i = 0; i < this.channels; ++i)
            {
                this.bufferp[i] = (short)i;
            }
        }

        #endregion

        #region Methods

        private void InitBlock()
        {
            this.myBuffer = new short[2];
        }

        #endregion

        /*
		* Create STDOUT buffer
		*
		*
		public static Obuffer create_stdout_obuffer(MPEG_Args maplay_args)
		{
		Obuffer thebuffer = null;
		int mode = maplay_args.MPEGheader.mode();
		int which_channels = maplay_args.which_c;
		if (mode == Header.single_channel || which_channels != MPEG_Args.both)
		thebuffer = new FileObuffer(1,maplay_args.output_filename);
		else
		thebuffer = new FileObuffer(2,maplay_args.output_filename);
		return(thebuffer);
		}
		*/
    }
}