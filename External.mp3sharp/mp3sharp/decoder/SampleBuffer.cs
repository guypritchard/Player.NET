/* 
* 12/12/99  Initial Version based on FileObuffer.	mdm@techie.com.
* 
* FileObuffer:
* 15/02/99 ,Java Conversion by E.B ,ebsp@iname.com, JavaLayer
*
*----------------------------------------------------------------------------- 
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*----------------------------------------------------------------------------
*/

namespace javazoom.jl.decoder
{
    /// <summary>
    ///     The <code>SampleBuffer</code> class implements an output buffer
    ///     that provides storage for a fixed size block of samples.
    /// </summary>
    internal class SampleBuffer : Obuffer
    {
        #region Fields

        private readonly short[] buffer;

        private readonly int[] bufferp;

        private readonly int channels;

        private readonly int frequency;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Constructor
        /// </summary>
        public SampleBuffer(int sample_frequency, int number_of_channels)
        {
            this.buffer = new short[OBUFFERSIZE];
            this.bufferp = new int[MAXCHANNELS];
            this.channels = number_of_channels;
            this.frequency = sample_frequency;

            for (int i = 0; i < number_of_channels; ++i)
            {
                this.bufferp[i] = (short)i;
            }
        }

        #endregion

        #region Public Properties

        public virtual short[] Buffer
        {
            get
            {
                return this.buffer;
            }
        }

        public virtual int BufferLength
        {
            get
            {
                return this.bufferp[0];
            }
        }

        public virtual int ChannelCount
        {
            get
            {
                return this.channels;
            }
        }

        public virtual int SampleFrequency
        {
            get
            {
                return this.frequency;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public override void Append(int channel, short value_Renamed)
        {
            this.buffer[this.bufferp[channel]] = value_Renamed;
            this.bufferp[channel] += this.channels;
        }

        public override void AppendSamples(int channel, float[] f)
        {
            int pos = this.bufferp[channel];

            for (int i = 0; i < 32;)
            {
                float fs = f[i++];

                if (fs > 32767.0f)
                {
                    fs = 32767.0f;
                }
                else
                {
                    if (fs < -32767.0f)
                    {
                        fs = -32767.0f;
                    }
                    else
                    {
                        fs = fs;
                    }
                }

                var s = (short)fs;
                this.buffer[pos] = s;
                pos += this.channels;
            }

            this.bufferp[channel] = pos;
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void ClearBuffer()
        {
            for (int i = 0; i < this.channels; ++i)
            {
                this.bufferp[i] = (short)i;
            }
        }

        public override void Close()
        {
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void SetStopFlag()
        {
        }

        /// <summary>
        ///     Write the samples to the file (Random Acces).
        /// </summary>
        public override void WriteBuffer(int val)
        {
            //for (int i = 0; i < channels; ++i) 
            //	bufferp[i] = (short)i;
        }

        #endregion
    }
}