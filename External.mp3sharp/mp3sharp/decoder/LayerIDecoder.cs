/*
* 12/12/99		Initial version. Adapted from javalayer.java
*				and Subband*.java. mdm@techie.com
/*-----------------------------------------------------------------------
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
*----------------------------------------------------------------------
*/

namespace javazoom.jl.decoder
{
    /// <summary>
    ///     Implements decoding of MPEG Audio Layer I frames.
    /// </summary>
    public class LayerIDecoder : IFrameDecoder
    {
        #region Fields

        protected internal Obuffer buffer;

        protected internal Crc16 crc = null;

        protected internal SynthesisFilter filter1, filter2;

        protected internal Header header;

        protected internal int mode;

        protected internal int num_subbands;

        protected internal Bitstream stream;

        protected internal Subband[] subbands;

        protected internal int which_channels;

        #endregion

        #region Constructors and Destructors

        public LayerIDecoder()
        {
            this.crc = new Crc16();
        }

        #endregion

        #region Public Methods and Operators

        public virtual void DecodeFrame()
        {
            this.num_subbands = this.header.NumberOfSubbands();
            this.subbands = new Subband[32];
            this.mode = this.header.Mode();

            this.CreateSubbands();

            this.ReadAllocation();
            this.ReadScaleFactorSelection();

            if ((this.crc != null) || this.header.CheckSumOK())
            {
                this.ReadScaleFactors();

                this.ReadSampleData();
            }
        }

        public virtual void create(
            Bitstream stream0,
            Header header0,
            SynthesisFilter filtera,
            SynthesisFilter filterb,
            Obuffer buffer0,
            int which_ch0)
        {
            this.stream = stream0;
            this.header = header0;
            this.filter1 = filtera;
            this.filter2 = filterb;
            this.buffer = buffer0;
            this.which_channels = which_ch0;
        }

        #endregion

        #region Methods

        protected internal virtual void CreateSubbands()
        {
            int i;
            if (this.mode == Header.SingleChannel)
            {
                for (i = 0; i < this.num_subbands; ++i)
                {
                    this.subbands[i] = new SubbandLayer1(i);
                }
            }
            else if (this.mode == Header.JointStereo)
            {
                for (i = 0; i < this.header.IntensityStereoBound(); ++i)
                {
                    this.subbands[i] = new SubbandLayer1Stereo(i);
                }
                for (; i < this.num_subbands; ++i)
                {
                    this.subbands[i] = new SubbandLayer1IntensityStereo(i);
                }
            }
            else
            {
                for (i = 0; i < this.num_subbands; ++i)
                {
                    this.subbands[i] = new SubbandLayer1Stereo(i);
                }
            }
        }

        protected internal virtual void ReadAllocation()
        {
            // start to read audio data:
            for (int i = 0; i < this.num_subbands; ++i)
            {
                this.subbands[i].read_allocation(this.stream, this.header, this.crc);
            }
        }

        protected internal virtual void ReadSampleData()
        {
            bool read_ready = false;
            bool write_ready = false;
            int mode = this.header.Mode();
            int i;
            do
            {
                for (i = 0; i < this.num_subbands; ++i)
                {
                    read_ready = this.subbands[i].read_sampledata(this.stream);
                }
                do
                {
                    for (i = 0; i < this.num_subbands; ++i)
                    {
                        write_ready = this.subbands[i].put_next_sample(this.which_channels, this.filter1, this.filter2);
                    }

                    this.filter1.calculate_pcm_samples(this.buffer);
                    if ((this.which_channels == OutputChannels.BOTH_CHANNELS) && (mode != Header.SingleChannel))
                    {
                        this.filter2.calculate_pcm_samples(this.buffer);
                    }
                }
                while (!write_ready);
            }
            while (!read_ready);
        }

        protected internal virtual void ReadScaleFactorSelection()
        {
            // scale factor selection not present for layer I. 
        }

        protected internal virtual void ReadScaleFactors()
        {
            for (int i = 0; i < this.num_subbands; ++i)
            {
                this.subbands[i].read_scalefactor(this.stream, this.header);
            }
        }

        #endregion

        // new Crc16[1] to enable CRC checking.

        /// <summary>
        ///     Abstract base class for subband classes of layer I and II
        /// </summary>
        public abstract class Subband
        {
            #region Static Fields

            public static readonly float[] scalefactors = new[]
                                                              {
                                                                  2.00000000000000f, 1.58740105196820f, 1.25992104989487f,
                                                                  1.00000000000000f, 0.79370052598410f, 0.62996052494744f,
                                                                  0.50000000000000f, 0.39685026299205f, 0.31498026247372f,
                                                                  0.25000000000000f, 0.19842513149602f, 0.15749013123686f,
                                                                  0.12500000000000f, 0.09921256574801f, 0.07874506561843f,
                                                                  0.06250000000000f, 0.04960628287401f, 0.03937253280921f,
                                                                  0.03125000000000f, 0.02480314143700f, 0.01968626640461f,
                                                                  0.01562500000000f, 0.01240157071850f, 0.00984313320230f,
                                                                  0.00781250000000f, 0.00620078535925f, 0.00492156660115f,
                                                                  0.00390625000000f, 0.00310039267963f, 0.00246078330058f,
                                                                  0.00195312500000f, 0.00155019633981f, 0.00123039165029f,
                                                                  0.00097656250000f, 0.00077509816991f, 0.00061519582514f,
                                                                  0.00048828125000f, 0.00038754908495f, 0.00030759791257f,
                                                                  0.00024414062500f, 0.00019377454248f, 0.00015379895629f,
                                                                  0.00012207031250f, 0.00009688727124f, 0.00007689947814f,
                                                                  0.00006103515625f, 0.00004844363562f, 0.00003844973907f,
                                                                  0.00003051757813f, 0.00002422181781f, 0.00001922486954f,
                                                                  0.00001525878906f, 0.00001211090890f, 0.00000961243477f,
                                                                  0.00000762939453f, 0.00000605545445f, 0.00000480621738f,
                                                                  0.00000381469727f, 0.00000302772723f, 0.00000240310869f,
                                                                  0.00000190734863f, 0.00000151386361f, 0.00000120155435f,
                                                                  0.00000000000000f
                                                              };

            #endregion

            #region Public Methods and Operators

            public abstract bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2);

            public abstract void read_allocation(Bitstream stream, Header header, Crc16 crc);

            public abstract bool read_sampledata(Bitstream stream);

            public abstract void read_scalefactor(Bitstream stream, Header header);

            #endregion

            /*
			*  Changes from version 1.1 to 1.2:
			*    - array size increased by one, although a scalefactor with index 63
			*      is illegal (to prevent segmentation faults)
			*/
            // Scalefactors for layer I and II, Annex 3-B.1 in ISO/IEC DIS 11172:
        }

        /// <summary>
        ///     Class for layer I subbands in single channel mode.
        ///     Used for single channel mode
        ///     and in derived class for intensity stereo mode
        /// </summary>
        internal class SubbandLayer1 : Subband
        {
            #region Static Fields

            public static readonly float[] table_factor = new[]
                                                              {
                                                                  0.0f, (1.0f / 2.0f) * (4.0f / 3.0f),
                                                                  (1.0f / 4.0f) * (8.0f / 7.0f),
                                                                  (1.0f / 8.0f) * (16.0f / 15.0f),
                                                                  (1.0f / 16.0f) * (32.0f / 31.0f),
                                                                  (1.0f / 32.0f) * (64.0f / 63.0f),
                                                                  (1.0f / 64.0f) * (128.0f / 127.0f),
                                                                  (1.0f / 128.0f) * (256.0f / 255.0f),
                                                                  (1.0f / 256.0f) * (512.0f / 511.0f),
                                                                  (1.0f / 512.0f) * (1024.0f / 1023.0f),
                                                                  (1.0f / 1024.0f) * (2048.0f / 2047.0f),
                                                                  (1.0f / 2048.0f) * (4096.0f / 4095.0f),
                                                                  (1.0f / 4096.0f) * (8192.0f / 8191.0f),
                                                                  (1.0f / 8192.0f) * (16384.0f / 16383.0f),
                                                                  (1.0f / 16384.0f) * (32768.0f / 32767.0f)
                                                              };

            public static readonly float[] table_offset = new[]
                                                              {
                                                                  0.0f, ((1.0f / 2.0f) - 1.0f) * (4.0f / 3.0f),
                                                                  ((1.0f / 4.0f) - 1.0f) * (8.0f / 7.0f),
                                                                  ((1.0f / 8.0f) - 1.0f) * (16.0f / 15.0f),
                                                                  ((1.0f / 16.0f) - 1.0f) * (32.0f / 31.0f),
                                                                  ((1.0f / 32.0f) - 1.0f) * (64.0f / 63.0f),
                                                                  ((1.0f / 64.0f) - 1.0f) * (128.0f / 127.0f),
                                                                  ((1.0f / 128.0f) - 1.0f) * (256.0f / 255.0f),
                                                                  ((1.0f / 256.0f) - 1.0f) * (512.0f / 511.0f),
                                                                  ((1.0f / 512.0f) - 1.0f) * (1024.0f / 1023.0f),
                                                                  ((1.0f / 1024.0f) - 1.0f) * (2048.0f / 2047.0f),
                                                                  ((1.0f / 2048.0f) - 1.0f) * (4096.0f / 4095.0f),
                                                                  ((1.0f / 4096.0f) - 1.0f) * (8192.0f / 8191.0f),
                                                                  ((1.0f / 8192.0f) - 1.0f) * (16384.0f / 16383.0f),
                                                                  ((1.0f / 16384.0f) - 1.0f) * (32768.0f / 32767.0f)
                                                              };

            #endregion

            #region Fields

            protected internal int allocation;

            protected internal float factor, offset;

            protected internal float sample;

            protected internal int samplelength;

            protected internal int samplenumber;

            protected internal float scalefactor;

            protected internal int subbandnumber;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Construtor.
            /// </summary>
            public SubbandLayer1(int subbandnumber)
            {
                this.subbandnumber = subbandnumber;
                this.samplenumber = 0;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     *
            /// </summary>
            public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
            {
                if ((this.allocation != 0) && (channels != OutputChannels.RIGHT_CHANNEL))
                {
                    float scaled_sample = (this.sample * this.factor + this.offset) * this.scalefactor;
                    filter1.input_sample(scaled_sample, this.subbandnumber);
                }
                return true;
            }

            /// <summary>
            ///     *
            /// </summary>
            public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
            {
                if ((this.allocation = stream.GetBits(4)) == 15)
                {
                }
                //	 cerr << "WARNING: stream contains an illegal allocation!\n";
                // MPEG-stream is corrupted!
                if (crc != null)
                {
                    crc.AddBits(this.allocation, 4);
                }
                if (this.allocation != 0)
                {
                    this.samplelength = this.allocation + 1;
                    this.factor = table_factor[this.allocation];
                    this.offset = table_offset[this.allocation];
                }
            }

            /// <summary>
            ///     *
            /// </summary>
            public override bool read_sampledata(Bitstream stream)
            {
                if (this.allocation != 0)
                {
                    this.sample = (stream.GetBits(this.samplelength));
                }
                if (++this.samplenumber == 12)
                {
                    this.samplenumber = 0;
                    return true;
                }
                return false;
            }

            /// <summary>
            ///     *
            /// </summary>
            public override void read_scalefactor(Bitstream stream, Header header)
            {
                if (this.allocation != 0)
                {
                    this.scalefactor = scalefactors[stream.GetBits(6)];
                }
            }

            #endregion

            // Factors and offsets for sample requantization
        }

        /// <summary>
        ///     Class for layer I subbands in joint stereo mode.
        /// </summary>
        internal class SubbandLayer1IntensityStereo : SubbandLayer1
        {
            #region Fields

            protected internal float channel2_scalefactor;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Constructor
            /// </summary>
            public SubbandLayer1IntensityStereo(int subbandnumber)
                : base(subbandnumber)
            {
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     *
            /// </summary>
            public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
            {
                if (this.allocation != 0)
                {
                    this.sample = this.sample * this.factor + this.offset; // requantization
                    if (channels == OutputChannels.BOTH_CHANNELS)
                    {
                        float sample1 = this.sample * this.scalefactor,
                              sample2 = this.sample * this.channel2_scalefactor;
                        filter1.input_sample(sample1, this.subbandnumber);
                        filter2.input_sample(sample2, this.subbandnumber);
                    }
                    else if (channels == OutputChannels.LEFT_CHANNEL)
                    {
                        float sample1 = this.sample * this.scalefactor;
                        filter1.input_sample(sample1, this.subbandnumber);
                    }
                    else
                    {
                        float sample2 = this.sample * this.channel2_scalefactor;
                        filter1.input_sample(sample2, this.subbandnumber);
                    }
                }
                return true;
            }

            /// <summary>
            ///     *
            /// </summary>
            public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
            {
                base.read_allocation(stream, header, crc);
            }

            /// <summary>
            ///     *
            /// </summary>
            public override bool read_sampledata(Bitstream stream)
            {
                return base.read_sampledata(stream);
            }

            /// <summary>
            ///     *
            /// </summary>
            public override void read_scalefactor(Bitstream stream, Header header)
            {
                if (this.allocation != 0)
                {
                    this.scalefactor = scalefactors[stream.GetBits(6)];
                    this.channel2_scalefactor = scalefactors[stream.GetBits(6)];
                }
            }

            #endregion
        }

        /// <summary>
        ///     Class for layer I subbands in stereo mode.
        /// </summary>
        internal class SubbandLayer1Stereo : SubbandLayer1
        {
            #region Fields

            protected internal int channel2_allocation;

            protected internal float channel2_factor, channel2_offset;

            protected internal float channel2_sample;

            protected internal int channel2_samplelength;

            protected internal float channel2_scalefactor;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Constructor
            /// </summary>
            public SubbandLayer1Stereo(int subbandnumber)
                : base(subbandnumber)
            {
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     *
            /// </summary>
            public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
            {
                base.put_next_sample(channels, filter1, filter2);
                if ((this.channel2_allocation != 0) && (channels != OutputChannels.LEFT_CHANNEL))
                {
                    float sample2 = (this.channel2_sample * this.channel2_factor + this.channel2_offset)
                                    * this.channel2_scalefactor;
                    if (channels == OutputChannels.BOTH_CHANNELS)
                    {
                        filter2.input_sample(sample2, this.subbandnumber);
                    }
                    else
                    {
                        filter1.input_sample(sample2, this.subbandnumber);
                    }
                }
                return true;
            }

            /// <summary>
            ///     *
            /// </summary>
            public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
            {
                this.allocation = stream.GetBits(4);

                if (this.allocation == 15)
                {
                    throw new DecoderException(DecoderErrors.IllegalSubBandAllocation, null);
                }

                this.channel2_allocation = stream.GetBits(4);
                if (this.channel2_allocation == 15)
                {
                    throw new DecoderException(DecoderErrors.IllegalSubBandAllocation, null);
                }

                if (crc != null)
                {
                    crc.AddBits(this.allocation, 4);
                    crc.AddBits(this.channel2_allocation, 4);
                }
                if (this.allocation != 0)
                {
                    this.samplelength = this.allocation + 1;
                    this.factor = table_factor[this.allocation];
                    this.offset = table_offset[this.allocation];
                }
                if (this.channel2_allocation != 0)
                {
                    this.channel2_samplelength = this.channel2_allocation + 1;
                    this.channel2_factor = table_factor[this.channel2_allocation];
                    this.channel2_offset = table_offset[this.channel2_allocation];
                }
            }

            /// <summary>
            ///     *
            /// </summary>
            public override bool read_sampledata(Bitstream stream)
            {
                bool returnvalue = base.read_sampledata(stream);
                if (this.channel2_allocation != 0)
                {
                    this.channel2_sample = (stream.GetBits(this.channel2_samplelength));
                }
                return (returnvalue);
            }

            /// <summary>
            ///     *
            /// </summary>
            public override void read_scalefactor(Bitstream stream, Header header)
            {
                if (this.allocation != 0)
                {
                    this.scalefactor = scalefactors[stream.GetBits(6)];
                }
                if (this.channel2_allocation != 0)
                {
                    this.channel2_scalefactor = scalefactors[stream.GetBits(6)];
                }
            }

            #endregion
        }


        public void SeekNotify()
        {
            throw new System.NotImplementedException();
        }
    }
}