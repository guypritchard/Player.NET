/*
* 1/12/99		Initial version.	mdm@techie.com
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
    using System;

    /// <summary>
    ///     The <code>Decoder</code> class encapsulates the details of
    ///     decoding an MPEG audio frame.
    /// </summary>
    /// <author>
    ///     MDM
    /// </author>
    /// <version>
    ///     0.0.7 12/12/99
    ///     @since	0.0.5
    /// </version>
    internal class Decoder
    {
        #region Fields

        private readonly Params params_Renamed;

        private Equalizer equalizer;

        /// <summary>
        ///     Synthesis filter for the left channel.
        /// </summary>
        private SynthesisFilter filter1;

        /// <summary>
        ///     Sythesis filter for the right channel.
        /// </summary>
        private SynthesisFilter filter2;

        private bool initialized;

        private LayerIDecoder l1decoder;

        private LayerIIDecoder l2decoder;

        /// <summary>
        ///     The decoder used to decode layer III frames.
        /// </summary>
        private LayerIIIDecoder l3decoder;

        /// <summary>
        ///     The Obuffer instance that will receive the decoded PCM samples.
        /// </summary>
        private Obuffer output;

        private int outputChannels;

        private int outputFrequency;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Creates a new <code>Decoder</code> instance with default
        ///     parameters.
        /// </summary>
        /// <param name="params	The">
        ///     <code>Params</code> instance that describes
        ///     the customizable aspects of the decoder.
        /// </param>
        public Decoder(Params params0 = null)
        {
            this.InitBlock();
            if (params0 == null)
            {
                params0 = this.DefaultParams;
            }

            this.params_Renamed = params0;

            Equalizer eq = this.params_Renamed.InitialEqualizerSettings;
            if (eq != null)
            {
                this.equalizer.FromEqualizer = eq;
            }
        }

        #endregion

        #region Public Properties

        public Params DefaultParams { get; set; }

        public virtual Equalizer Equalizer
        {
            set
            {
                if (value == null)
                {
                    value = Equalizer.PassThruEq;
                }

                this.equalizer.FromEqualizer = value;

                float[] factors = this.equalizer.BandFactors;
                if (this.filter1 != null)
                {
                    this.filter1.EQ = factors;
                }

                if (this.filter2 != null)
                {
                    this.filter2.EQ = factors;
                }
            }
        }

        /// <summary>
        ///     Retrieves the maximum number of samples that will be written to
        ///     the output buffer when one frame is decoded. This can be used to
        ///     help calculate the size of other buffers whose size is based upon
        ///     the number of samples written to the output buffer. NB: this is
        ///     an upper bound and fewer samples may actually be written, depending
        ///     upon the sample rate and number of channels.
        /// </summary>
        /// <returns>
        ///     The maximum number of samples that are written to the
        ///     output buffer when decoding a single frame of MPEG audio.
        /// </returns>
        public virtual int OutputBlockSize
        {
            get
            {
                return Obuffer.OBUFFERSIZE;
            }
        }

        /// <summary>
        ///     Changes the output buffer. This will take effect the next time
        ///     decodeFrame() is called.
        /// </summary>
        public virtual Obuffer OutputBuffer
        {
            set
            {
                this.output = value;
            }
        }

        /// <summary>
        ///     Retrieves the number of channels of PCM samples output by
        ///     this decoder. This usually corresponds to the number of
        ///     channels in the MPEG audio stream, although it may differ.
        /// </summary>
        /// <returns>
        ///     The number of output channels in the decoded samples: 1
        ///     for mono, or 2 for stereo.
        /// </returns>
        public virtual int OutputChannels
        {
            get
            {
                return this.outputChannels;
            }
        }

        /// <summary>
        ///     Retrieves the sample frequency of the PCM samples output
        ///     by this decoder. This typically corresponds to the sample
        ///     rate encoded in the MPEG audio stream.
        /// </summary>
        /// <param name="the">
        ///     sample rate (in Hz) of the samples written to the
        ///     output buffer when decoding.
        /// </param>
        public virtual int OutputFrequency
        {
            get
            {
                return this.outputFrequency;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Decodes one frame from an MPEG audio bitstream.
        /// </summary>
        /// <param name="header">
        ///     header describing the frame to decode.
        /// </param>
        /// <param name="bitstream">
        ///     Bitstream that provides the bits for the body of the frame.
        /// </param>
        /// <returns>
        ///     A SampleBuffer containing the decoded samples.
        /// </returns>
        public virtual Obuffer DecodeFrame(Header header, Bitstream stream)
        {
            lock (this)
            {
                if (!this.initialized)
                {
                    this.Initialize(header);
                }

                this.output.ClearBuffer();

                IFrameDecoder decoder = this.RetrieveDecoder(header, stream, header.Layer());

                decoder.DecodeFrame();

                this.output.WriteBuffer(1);

                return this.output;
            }
        }

        #endregion

        #region Methods

        protected internal virtual IFrameDecoder RetrieveDecoder(Header header, Bitstream stream, int layer)
        {
            IFrameDecoder decoder = null;
            switch (layer)
            {
                case 3:
                    this.l3decoder = this.l3decoder ?? new LayerIIIDecoder(
                            stream,
                            header,
                            this.filter1,
                            this.filter2,
                            this.output,
                            (int)OutputChannelsEnum.BothChannels);
                    decoder = this.l3decoder;
                    break;

                case 2:
                    if (this.l2decoder == null)
                    {
                        this.l2decoder = new LayerIIDecoder();
                        this.l2decoder.create(
                            stream,
                            header,
                            this.filter1,
                            this.filter2,
                            this.output,
                            (int)OutputChannelsEnum.BothChannels);
                    }
                    decoder = this.l2decoder;
                    break;

                case 1:
                    if (this.l1decoder == null)
                    {
                        this.l1decoder = new LayerIDecoder();
                        this.l1decoder.create(
                            stream,
                            header,
                            this.filter1,
                            this.filter2,
                            this.output,
                            (int)OutputChannelsEnum.BothChannels);
                    }
                    decoder = this.l1decoder;
                    break;
            }

            if (decoder == null)
            {
                throw new DecoderException(DecoderErrors.UnsupportedLayer, null);
            }

            return decoder;
        }

        private void InitBlock()
        {
            this.equalizer = new Equalizer();
            this.DefaultParams = new Params();
        }

        private void Initialize(Header header)
        {
            // REVIEW: allow customizable scale factor
            float scalefactor = 32700.0f;

            int mode = header.Mode();
            int layer = header.Layer();
            int channels = mode == Header.SingleChannel ? 1 : 2;

            // set up output buffer if not set up by client.
            if (this.output == null)
            {
                this.output = new SampleBuffer(header.Frequency(), channels);
            }

            float[] factors = this.equalizer.BandFactors;
            this.filter1 = new SynthesisFilter(0, scalefactor, factors);

            if (channels == 2)
            {
                this.filter2 = new SynthesisFilter(1, scalefactor, factors);
            }

            this.outputChannels = channels;
            this.outputFrequency = header.Frequency();

            this.initialized = true;
        }

        #endregion

        /// <summary>
        ///     The <code>Params</code> class presents the customizable
        ///     aspects of the decoder.
        ///     Instances of this class are not thread safe.
        /// </summary>
        internal class Params : ICloneable
        {
            #region Fields

            private Equalizer equalizer;

            private OutputChannels outputChannels;

            #endregion

            #region Public Properties

            /// <summary>
            ///     Retrieves the equalizer settings that the decoder's equalizer
            ///     will be initialized from.
            ///     The <code>Equalizer</code> instance returned
            ///     cannot be changed in real time to affect the
            ///     decoder output as it is used only to initialize the decoders
            ///     EQ settings. To affect the decoder's output in realtime,
            ///     use the Equalizer returned from the getEqualizer() method on the decoder.
            /// </summary>
            /// <returns>
            ///     The <code>Equalizer</code> used to initialize the  EQ settings of the decoder.
            /// </returns>
            public virtual Equalizer InitialEqualizerSettings
            {
                get
                {
                    return this.equalizer;
                }
            }

            public virtual OutputChannels OutputChannels
            {
                get
                {
                    return this.outputChannels;
                }

                set
                {
                    if (value == null)
                    {
                        throw new NullReferenceException("out");
                    }

                    this.outputChannels = value;
                }
            }

            #endregion

            #region Public Methods and Operators

            public object Clone()
            {
                return this.MemberwiseClone();
            }

            #endregion
        }
    }
}