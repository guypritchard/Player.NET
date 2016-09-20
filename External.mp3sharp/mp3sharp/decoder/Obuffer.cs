/* 
* 12/12/99  Added appendSamples() method for efficiency. MDM.
* 15/02/99 ,Java Conversion by E.B ,ebsp@iname.com, JavaLayer
*----------------------------------------------------------------------------- 
* obuffer.h
*
*    Declarations for output buffer, includes operating system
*   implementation of the virtual Obuffer. Optional routines
*   enabling seeks and stops added by Jeff Tsay. 
*   
*
*  @(#) obuffer.h 1.8, last edit: 6/15/94 16:51:56
*  @(#) Copyright (C) 1993, 1994 Tobias Bading (bading@cs.tu-berlin.de)
*  @(#) Berlin University of Technology
*
*  Idea and first implementation for u-law output with fast downsampling by
*  Jim Boucher (jboucher@flash.bu.edu)
*
*  LinuxObuffer class written by
*  Louis P. Kruger (lpkruger@phoenix.princeton.edu)
*
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
    ///     Base Class for audio output.
    /// </summary>
    public abstract class Obuffer
    {
        public const int MAXCHANNELS = 2; // max. number of channels

        public const int OBUFFERSIZE = 2 * 1152; // max. 2 * 1152 samples per frame

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public abstract void Append(int channel, short value_Renamed);

        /// <summary>
        ///     Accepts 32 new PCM samples.
        /// </summary>
        public virtual void AppendSamples(int channel, float[] f)
        {
            short s;
            for (int i = 0; i < 32; i++)
            {
                this.Append(channel, Clip((f[i])));
            }
        }

        /// <summary>
        ///     Clears all data in the buffer (for seeking).
        /// </summary>
        public abstract void ClearBuffer();

        public abstract void Close();

        /// <summary>
        ///     Notify the buffer that the user has stopped the stream.
        /// </summary>
        public abstract void SetStopFlag();

        /// <summary>
        ///     Write the samples to the file or directly to the audio hardware.
        /// </summary>
        public abstract void WriteBuffer(int val);

        /// <summary>
        ///     Clip Sample to 16 Bits
        /// </summary>
        private static short Clip(float sample)
        {
            if (sample > short.MaxValue) // can this happen?
            {
                return short.MaxValue;
            }
            else if (sample < short.MinValue)
            {
                return short.MinValue;
            }

            return (short)sample;
        }
    }
}