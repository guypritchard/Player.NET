namespace Mp3Sharp
{
    using System;
    using System.Diagnostics;
    using javazoom.jl.decoder;

    /// <summary>
    ///     Internal class used to queue samples that are being obtained
    ///     from an Mp3 stream. This merges the old mp3stream OBuffer with
    ///     the javazoom SampleBuffer code for the highest efficiency...
    ///     well, not the highest possible. The highest I'm willing to sweat
    ///     over. --trs
    ///     This class handles stereo 16-bit data! Switch it out if you want mono or something.
    /// </summary>
    internal class OBuffer16BitStereo : Obuffer
    {
        #region Static Fields

        private static readonly int CHANNELS = 2;

        #endregion

        #region Fields

        private readonly byte[] buffer = new byte[OBUFFERSIZE * 2]; // all channels interleaved

        private readonly int[] bufferp = new int[MAXCHANNELS]; // offset in each channel not same!

        private int _end;

        private int _offset;

        #endregion

        #region Constructors and Destructors

        public OBuffer16BitStereo()
        {
            // Initialize the buffer pointers
            this.ClearBuffer();
        }

        #endregion

        #region Public Properties

        public int bytesLeft
        {
            get
            {
                lock (this)
                {
                    // Note: should be Math.Max( bufferp[0], bufferp[1]-1 ). 
                    // Heh.
                    return this._end - this._offset;

                    // This results in a measurable performance improvement, but
                    // is actually incorrect. Is there a trick to optimize this?
                    // return (OBUFFERSIZE * 2) - _offset;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        public override void Append(int channel, short value)
        {
            lock (this)
            {
                this.buffer[this.bufferp[channel]] = (byte) (value & 0xff);
                this.buffer[this.bufferp[channel] + 1] = (byte) (value >> 8);

                this.bufferp[channel] += CHANNELS*2;

                Debug.WriteLine(channel + ":" + this.bufferp[channel]);
            }

        }

        // efficiently write 32 samples
        public override void AppendSamples(int channel, float[] f)
        {
            lock (this)
            {

                // Always, 32 samples are appended
                int pos = this.bufferp[channel];

                // Trace.WriteLine(string.Format("Enter Channel:{0} Pos:{1}", channel, pos));

                short s;
                for (int i = 0; i < 32; i++)
                {
                    float fs = f[i];

                    if (fs > short.MaxValue) // can this happen?
                    {
                        fs = short.MaxValue;
                    }
                    else if (fs < short.MinValue)
                    {
                        fs = short.MinValue;
                    }

                    var sample = (int) fs;
                    this.buffer[pos] = (byte) (sample & 0xff);
                    this.buffer[pos + 1] = (byte) (sample >> 8);

                    pos += CHANNELS*2;
                }

                //if (pos >= this.buffer.Length)
                //{
                //    return;
                //}

                // Trace.WriteLine(string.Format("Exit Channel:{0} Pos:{1}", channel, pos));
                this.bufferp[channel] = pos;
            }
        }

        /// Copies as much of this buffer as will fit into the output
        /// buffer.
        /// 
        /// return The amount of bytes copied.
        public int Read(byte[] buffer_out, int offset, int count)
        {
            lock (this)
            {
                int remaining = this.bytesLeft;
                int copySize;
                if (count > remaining)
                {
                    copySize = remaining;
                }
                else
                {
                    // Copy an even number of sample frames
                    int remainder = count%(2*CHANNELS);
                    copySize = count - remainder;
                }

                Array.Copy(this.buffer, this._offset, buffer_out, offset, copySize);

                this._offset += copySize;
                return copySize;
            }
        }

        /// <summary>
        ///     This implementation does not clear the buffer.
        /// </summary>
        public override void ClearBuffer()
        {
            lock (this)
            {
                this._offset = 0;
                this._end = 0;


                for (int i = 0; i < CHANNELS; i++)
                {
                    this.bufferp[i] = i*2; // two bytes per channel
                }
            }
        }

        public override void Close()
        {
        }

        public override void SetStopFlag()
        {
        }

        public override void WriteBuffer(int val)
        {
            lock (this)
            {
                this._offset = 0;

                // speed optimization - save end marker, and avoid
                // array access at read time. Can you believe this saves
                // like 1-2% of the cpu on a PIII? I guess allocating
                // that temporary "new int(0)" is expensive, too.
                this._end = this.bufferp[0];
            }
        }

        #endregion

        // This is stereo!

        // Read offset used to read from the stream, in bytes.

        // Write offset used in append_bytes

        // Inefficiently write one sample value
    }
}