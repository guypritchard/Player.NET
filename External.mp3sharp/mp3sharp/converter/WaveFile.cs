/*
* 02/23/99 JavaConversion by E.B, JavaLayer
*/
/*===========================================================================

riff.h  -  Don Cross, April 1993.

RIFF file format classes.
See Chapter 8 of "Multimedia Programmer's Reference" in
the Microsoft Windows SDK.

See also:
..\source\riff.cpp
ddc.h

===========================================================================*/

namespace javazoom.jl.converter
{
    using System;
    using System.IO;

    /// <summary>
    ///     Class allowing WaveFormat Access
    /// </summary>
    internal class WaveFile : RiffFile
    {
        #region Constants

        public const int MAX_WAVE_CHANNELS = 2;

        #endregion

        #region Fields

        private readonly int num_samples;

        private readonly RiffChunkHeader pcm_data;

        private readonly WaveFormatChunk wave_format;

        private bool JustWriteLengthBytes;

        private long pcm_data_offset; // offset of 'pcm_data' in output file

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Constructs a new WaveFile instance.
        /// </summary>
        public WaveFile()
        {
            this.pcm_data = new RiffChunkHeader(this);
            this.wave_format = new WaveFormatChunk(this);
            this.pcm_data.ckID = FourCC("data");
            this.pcm_data.ckSize = 0;
            this.num_samples = 0;
        }

        #endregion

        #region Public Methods and Operators

        public virtual short BitsPerSample()
        {
            return this.wave_format.data.BitsPerSample;
        }

        /// <summary>
        /// </summary>
        public override int Close()
        {
            int rc = DDC_SUCCESS;

            if (this.fmode == RFM_WRITE)
            {
                rc = this.Backpatch(this.pcm_data_offset, this.pcm_data, 8);
            }
            if (!this.JustWriteLengthBytes)
            {
                if (rc == DDC_SUCCESS)
                {
                    rc = base.Close();
                }
            }
            return rc;
        }

        public int Close(bool justWriteLengthBytes)
        {
            this.JustWriteLengthBytes = justWriteLengthBytes;
            int ret = this.Close();
            this.JustWriteLengthBytes = false;
            return ret;
        }

        public virtual short NumChannels()
        {
            return this.wave_format.data.Channels;
        }

        public virtual int NumSamples()
        {
            return this.num_samples;
        }

        /// <summary>
        ///     Pass in either a FileName or a Stream.
        /// </summary>
        public virtual int OpenForWrite(
            String Filename, Stream stream, int SamplingRate, short BitsPerSample, short NumChannels)
        {
            // Verify parameters...
            if ((BitsPerSample != 8 && BitsPerSample != 16) || NumChannels < 1 || NumChannels > 2)
            {
                return DDC_INVALID_CALL;
            }

            this.wave_format.data.Config(SamplingRate, BitsPerSample, NumChannels);

            int retcode = 0;
            if (stream != null)
            {
                Open(stream, RFM_WRITE);
            }
            else
            {
                Open(Filename, RFM_WRITE);
            }

            if (retcode == DDC_SUCCESS)
            {
                var theWave = new[] { (sbyte)'W', (sbyte)'A', (sbyte)'V', (sbyte)'E' };
                retcode = Write(theWave, 4);

                if (retcode == DDC_SUCCESS)
                {
                    // Ecriture de wave_format
                    retcode = this.Write(this.wave_format.header, 8);
                    retcode = this.Write(this.wave_format.data.FormatTag, 2);
                    retcode = this.Write(this.wave_format.data.Channels, 2);
                    retcode = this.Write(this.wave_format.data.SamplesPerSec, 4);
                    retcode = this.Write(this.wave_format.data.AvgBytesPerSec, 4);
                    retcode = this.Write(this.wave_format.data.BlockAlign, 2);
                    retcode = this.Write(this.wave_format.data.BitsPerSample, 2);

                    if (retcode == DDC_SUCCESS)
                    {
                        this.pcm_data_offset = this.CurrentFilePosition();
                        retcode = this.Write(this.pcm_data, 8);
                    }
                }
            }

            return retcode;
        }

        /// <summary>
        ///     Open for write using another wave file's parameters...
        /// </summary>
        public virtual int OpenForWrite(String Filename, WaveFile OtherWave)
        {
            return this.OpenForWrite(
                Filename, null, OtherWave.SamplingRate(), OtherWave.BitsPerSample(), OtherWave.NumChannels());
        }

        public virtual int SamplingRate()
        {
            return this.wave_format.data.SamplesPerSec;
        }

        /// <summary>
        ///     Write 16-bit audio
        /// </summary>
        public virtual int WriteData(short[] data, int numData)
        {
            int extraBytes = numData * 2;
            this.pcm_data.ckSize += extraBytes;
            return base.Write(data, extraBytes);
        }

        #endregion
    }
}