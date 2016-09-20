/*
* 02/13/99 : Java Conversion by E.B , ebsp@iname.com
*
*---------------------------------------------------------------------------
* Declarations for MPEG header class
* A few layer III, MPEG-2 LSF, and seeking modifications made by Jeff Tsay.
* Last modified : 04/19/97
*
*  @(#) header.h 1.7, last edit: 6/15/94 16:55:33
*  @(#) Copyright (C) 1993, 1994 Tobias Bading (bading@cs.tu-berlin.de)
*  @(#) Berlin University of Technology
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
*--------------------------------------------------------------------------
*/

namespace javazoom.jl.decoder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Remoting.Messaging;
    using System.Text;

    using ID3;

    using Mp3Sharp;

    using Support;

    /// <summary>
    ///     Class for extracting information from a frame header.
    /// </summary>
    public class Header
    {
        #region Constants

        public const int DualChannel = 2;

        public const int FourtyEight = 1;

        public const int FourtyFourPointOne = 0;

        public const int JointStereo = 1;

        public const int MPEG25_LSF = 2; // SZD

        /// <summary>
        ///     Constant for MPEG-2 LSF version
        /// </summary>
        public const int MPEG2_LSF = 0;

        /// <summary>
        ///     Constant for MPEG-1 version
        /// </summary>
        public const int Mpeg1 = 1;

        public const int SingleChannel = 3;

        public const int Stereo = 0;

        public const int ThirtyTwo = 2;

        #endregion

        #region Static Fields

        public static readonly float[][] msPerFrameArray =
                {
                    new[] { 8.707483f, 8.0f, 12.0f }, 
                    new[] { 26.12245f, 24.0f, 36.0f },
                    new[] { 26.12245f, 24.0f, 36.0f }
                };

        public static readonly string[][][] BitrateStr =
            {
                new[]
                    {
                        new[]
                            {
                                "free format", "32 kbit/s", "48 kbit/s",
                                "56 kbit/s", "64 kbit/s", "80 kbit/s",
                                "96 kbit/s", "112 kbit/s", "128 kbit/s",
                                "144 kbit/s", "160 kbit/s", "176 kbit/s",
                                "192 kbit/s", "224 kbit/s", "256 kbit/s",
                                "forbidden"
                            },
                        new[]
                            {
                                "free format", "8 kbit/s", "16 kbit/s",
                                "24 kbit/s", "32 kbit/s", "40 kbit/s",
                                "48 kbit/s", "56 kbit/s", "64 kbit/s",
                                "80 kbit/s", "96 kbit/s", "112 kbit/s",
                                "128 kbit/s", "144 kbit/s", "160 kbit/s",
                                "forbidden"
                            },
                        new[]
                            {
                                "free format", "8 kbit/s", "16 kbit/s",
                                "24 kbit/s", "32 kbit/s", "40 kbit/s",
                                "48 kbit/s", "56 kbit/s", "64 kbit/s",
                                "80 kbit/s", "96 kbit/s", "112 kbit/s",
                                "128 kbit/s", "144 kbit/s", "160 kbit/s",
                                "forbidden"
                            }
                    },
                new[]
                    {
                        new[]
                            {
                                "free format", "32 kbit/s", "64 kbit/s",
                                "96 kbit/s", "128 kbit/s", "160 kbit/s",
                                "192 kbit/s", "224 kbit/s", "256 kbit/s",
                                "288 kbit/s", "320 kbit/s", "352 kbit/s",
                                "384 kbit/s", "416 kbit/s", "448 kbit/s",
                                "forbidden"
                            },
                        new[]
                            {
                                "free format", "32 kbit/s", "48 kbit/s",
                                "56 kbit/s", "64 kbit/s", "80 kbit/s",
                                "96 kbit/s", "112 kbit/s", "128 kbit/s",
                                "160 kbit/s", "192 kbit/s", "224 kbit/s",
                                "256 kbit/s", "320 kbit/s", "384 kbit/s",
                                "forbidden"
                            },
                        new[]
                            {
                                "free format", "32 kbit/s", "40 kbit/s",
                                "48 kbit/s", "56 kbit/s", "64 kbit/s",
                                "80 kbit/s", "96 kbit/s", "112 kbit/s",
                                "128 kbit/s", "160 kbit/s", "192 kbit/s",
                                "224 kbit/s", "256 kbit/s", "320 kbit/s",
                                "forbidden"
                            }
                    },
                new[]
                    {
                        new[]
                            {
                                "free format", "32 kbit/s", "48 kbit/s",
                                "56 kbit/s", "64 kbit/s", "80 kbit/s",
                                "96 kbit/s", "112 kbit/s", "128 kbit/s",
                                "144 kbit/s", "160 kbit/s", "176 kbit/s",
                                "192 kbit/s", "224 kbit/s", "256 kbit/s",
                                "forbidden"
                            },
                        new[]
                            {
                                "free format", "8 kbit/s", "16 kbit/s",
                                "24 kbit/s", "32 kbit/s", "40 kbit/s",
                                "48 kbit/s", "56 kbit/s", "64 kbit/s",
                                "80 kbit/s", "96 kbit/s", "112 kbit/s",
                                "128 kbit/s", "144 kbit/s", "160 kbit/s",
                                "forbidden"
                            },
                        new[]
                            {
                                "free format", "8 kbit/s", "16 kbit/s",
                                "24 kbit/s", "32 kbit/s", "40 kbit/s",
                                "48 kbit/s", "56 kbit/s", "64 kbit/s",
                                "80 kbit/s", "96 kbit/s", "112 kbit/s",
                                "128 kbit/s", "144 kbit/s", "160 kbit/s",
                                "forbidden"
                            }
                    }
            };

        public static readonly int[][][] Bitrates =
            {
                new[]
                    {
                        new[]
                            {
                                0, 32000, 48000, 56000, 64000, 80000, 96000,
                                112000, 128000, 144000, 160000, 176000, 192000,
                                224000, 256000, 0
                            },
                        new[]
                            {
                                0, 8000, 16000, 24000, 32000, 40000, 48000, 56000,
                                64000, 80000, 96000, 112000, 128000, 144000,
                                160000, 0
                            },
                        new[]
                            {
                                0, 8000, 16000, 24000, 32000, 40000, 48000, 56000,
                                64000, 80000, 96000, 112000, 128000, 144000,
                                160000, 0
                            }
                    },
                new[]
                    {
                        new[]
                            {
                                0, 32000, 64000, 96000, 128000, 160000, 192000,
                                224000, 256000, 288000, 320000, 352000, 384000,
                                416000, 448000, 0
                            },
                        new[]
                            {
                                0, 32000, 48000, 56000, 64000, 80000, 96000,
                                112000, 128000, 160000, 192000, 224000, 256000,
                                320000, 384000, 0
                            },
                        new[]
                            {
                                0, 32000, 40000, 48000, 56000, 64000, 80000, 96000
                                , 112000, 128000, 160000, 192000, 224000, 256000,
                                320000, 0
                            }
                    },
                new[]
                    {
                        new[]
                            {
                                0, 32000, 48000, 56000, 64000, 80000, 96000,
                                112000, 128000, 144000, 160000, 176000, 192000,
                                224000, 256000, 0
                            },
                        new[]
                            {
                                0, 8000, 16000, 24000, 32000, 40000, 48000, 56000,
                                64000, 80000, 96000, 112000, 128000, 144000,
                                160000, 0
                            },
                        new[]
                            {
                                0, 8000, 16000, 24000, 32000, 40000, 48000, 56000,
                                64000, 80000, 96000, 112000, 128000, 144000,
                                160000, 0
                            }
                    }
            };

        public static readonly int[][] frequencies =
            {
                new[] { 22050, 24000, 16000, 1 },
                new[] { 44100, 48000, 32000, 1 }, new[] { 11025, 12000, 8000, 1 }
            }; // SZD: MPEG25

        #endregion

        #region Fields

        private int _headerstring = -1;

        public short checksum;

        private Crc16 crc;

        public int framesize;

        private int h_bitrate_index;

        private bool h_copyright;

        private int h_intensity_stereo_bound;

        private int h_layer;

        private int h_mode;

        private int h_mode_extension;

        private int h_number_of_subbands;

        private bool h_original;

        private int h_padding_bit;

        private int h_protection_bit;

        private int h_sample_frequency;

        // VBR support added by E.B

        private bool h_vbr;

        private int h_vbr_bytes;

        private int h_vbr_frames;

        private int h_vbr_scale;

        private double[] h_vbr_time_per_frame = { -1, 384, 1152, 1152 };

        private byte[] h_vbr_toc;

        private int h_version;

        private IList<Id3V2Frame> metadata;

        public int nSlots;

        private sbyte syncmode;

        #endregion

        #region Constructors and Destructors

        internal Header()
        {
            this.InitBlock();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Returns synchronized header.
        /// </summary>
        public virtual int SyncHeader
        {
            // E.B
            get
            {
                return this._headerstring;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Returns bitrate index.
        /// </summary>
        public int BitrateIndex()
        {
            return this.h_bitrate_index;
        }

        /// <summary>
        ///     Returns Bitrate.
        /// </summary>
        public string BitrateString()
        {
            return BitrateStr[this.h_version][this.h_layer - 1][this.h_bitrate_index];
        }

        public int Bitrate()
        {
            return Bitrates[this.h_version][this.h_layer - 1][this.h_bitrate_index];
        }

        /// <summary>
        ///     Calculate Frame size.
        ///     Calculates framesize in bytes excluding header size.
        /// </summary>
        public int CalculateFrameSize()
        {
            if (this.h_layer == 1)
            {
                this.framesize = (12 * Bitrates[this.h_version][0][this.h_bitrate_index])
                                 / frequencies[this.h_version][this.h_sample_frequency];
                if (this.h_padding_bit != 0)
                {
                    this.framesize++;
                }
                this.framesize <<= 2; // one slot is 4 bytes long
                this.nSlots = 0;
            }
            else
            {
                this.framesize = (144 * Bitrates[this.h_version][this.h_layer - 1][this.h_bitrate_index])
                                 / frequencies[this.h_version][this.h_sample_frequency];

                if (this.h_version == MPEG2_LSF || this.h_version == MPEG25_LSF)
                {
                    this.framesize >>= 1;
                }

                if (this.h_padding_bit != 0)
                {
                    this.framesize++;
                }

                // Layer III slots
                if (this.h_layer == 3)
                {
                    if (this.h_version == Mpeg1)
                    {
                        this.nSlots = this.framesize - ((this.h_mode == SingleChannel) ? 17 : 32)
                                      - ((this.h_protection_bit != 0) ? 0 : 2) - 4; // header size
                    }
                    else
                    {
                        // MPEG-2 LSF, SZD: MPEG-2.5 LSF
                        this.nSlots = this.framesize - ((this.h_mode == SingleChannel) ? 9 : 17)
                                      - ((this.h_protection_bit != 0) ? 0 : 2) - 4; // header size
                    }
                }
                else
                {
                    this.nSlots = 0;
                }
            }

            this.framesize -= 4; // subtract header size
            return this.framesize;
        }

        /// <summary>
        ///     Returns Checksum flag.
        ///     Compares computed checksum with stream checksum.
        /// </summary>
        public bool CheckSumOK()
        {
            return (this.checksum == this.crc.Checksum());
        }

        /// <summary>
        ///     Returns Protection bit.
        /// </summary>
        public bool CheckSums()
        {
            return this.h_protection_bit == 0;
        }

        /// <summary>
        ///     Returns Copyright.
        /// </summary>
        public bool Copyright()
        {
            return this.h_copyright;
        }

        /// <summary>
        ///     Returns Frequency.
        /// </summary>
        public int Frequency()
        {
            return frequencies[this.h_version][this.h_sample_frequency];
        }

        /// <summary>
        ///     Returns Intensity Stereo.
        ///     Layer II joint stereo only).
        ///     Returns the number of subbands which are in stereo mode,
        ///     subbands above that limit are in intensity stereo mode.
        /// </summary>
        public int IntensityStereoBound()
        {
            return this.h_intensity_stereo_bound;
        }

        /// <summary>
        ///     Returns Layer ID.
        /// </summary>
        public int Layer()
        {
            return this.h_layer;
        }

        /// <summary>
        ///     Return Layer version.
        /// </summary>
        public string LayerString()
        {
            switch (this.h_layer)
            {
                case 1:
                    return "I";

                case 2:
                    return "II";

                case 3:
                    return "III";
            }
            return null;
        }

        /// <summary>
        ///     Returns the maximum number of frames in the stream.
        /// </summary>
        public int MaxNumberOfFrames(int streamsize)
        {
            if ((this.framesize + 4 - this.h_padding_bit) == 0)
            {
                return 0;
            }

            return (streamsize / (this.framesize + 4 - this.h_padding_bit));
        }

        public IList<Id3V2Frame> Metadata()
        {
            return this.metadata;
        }

        /// <summary>
        ///     Returns the maximum number of frames in the stream.
        /// </summary>
        public int MinNumberOfFrames(int streamsize)
        {
            if ((this.framesize + 5 - this.h_padding_bit) == 0)
            {
                return 0;
            }

            return (streamsize / (this.framesize + 5 - this.h_padding_bit));
        }

        /// <summary>
        ///     Returns Mode.
        /// </summary>
        public int Mode()
        {
            return this.h_mode;
        }

        /// <summary>
        ///     Returns Mode Extension.
        /// </summary>
        public int ModeExtension()
        {
            return this.h_mode_extension;
        }

        /// <summary>
        ///     Returns Mode.
        /// </summary>
        public string ModeString()
        {
            switch (this.h_mode)
            {
                case Stereo:
                    return "Stereo";

                case JointStereo:
                    return "Joint stereo";

                case DualChannel:
                    return "Dual channel";

                case SingleChannel:
                    return "Single channel";
            }
            return null;
        }

        /// <summary>
        ///     Returns ms/frame.
        /// </summary>
        public float MsPerFrame()
        {
            return (msPerFrameArray[this.h_layer - 1][this.h_sample_frequency]);
        }

        /// <summary>
        ///     Returns the number of subbands in the current frame.
        /// </summary>
        public int NumberOfSubbands()
        {
            return this.h_number_of_subbands;
        }

        /// <summary>
        ///     Returns Original.
        /// </summary>
        public bool Original()
        {
            return this.h_original;
        }

        /// <summary>
        ///     Returns Layer III Padding bit.
        /// </summary>
        public bool Padding()
        {
            return this.h_padding_bit != 0;
        }

        public void ParseVbr(byte[] firstFrame)
        {
            try
            {
                ParseXingHeader(firstFrame);
            }
            catch (Exception e)
            {
                throw new BitstreamException("XingVBRHeader Corrupted", e);
            }
            
            try
            {
                ParseVBRIHeader(firstFrame);
            }
            catch (Exception e)
            {
                throw new BitstreamException("VBRIVBRHeader Corrupted", e);
            }
        }

        private void ParseVBRIHeader(byte[] firstFrame)
        {
            int offset = 36 - 4;
            var tmp = new byte[4];
            Array.Copy(firstFrame, offset, tmp, 0, 4);

            // Is "VBRI" ?
            if (string.Equals("VBRI", Encoding.ASCII.GetString(tmp)))
            {
                this.h_vbr = true;
                this.h_vbr_frames = -1;
                this.h_vbr_bytes = -1;
                this.h_vbr_scale = -1;
                this.h_vbr_toc = new byte[100];
                
                // Bytes.				
                int length = 4 + 6;
                Array.Copy(firstFrame, offset + length, tmp, 0, tmp.Length);
                this.h_vbr_bytes = (int) ((tmp[0] << 24) & 0xFF000000 | 
                                          (tmp[1] << 16) & 0x00FF0000 | 
                                          (tmp[2] << 8)  & 0x0000FF00 | 
                                           tmp[3]        & 0x000000FF);
                length += 4;

                // Frames.	
                Array.Copy(firstFrame, offset + length, tmp, 0, tmp.Length);
                this.h_vbr_frames = (int) ((tmp[0] << 24) & 0xFF000000 | 
                                           (tmp[1] << 16) & 0x00FF0000 | 
                                           (tmp[2] << 8)  & 0x0000FF00 | 
                                            tmp[3]        & 0x000000FF);
            }
        }

        private void ParseXingHeader(byte[] firstFrame)
        {
            // Compute "Xing" offset depending on MPEG version and channels.
            int offset = this.h_version == Mpeg1
                        ? (this.h_mode == SingleChannel ? 21 - 4 : 36 - 4)
                        : (this.h_mode == SingleChannel ? 13 - 4 : 21 - 4);


            var tmp = new byte[4];
            Array.Copy(firstFrame, offset, tmp, 0, 4);
            string xing = "Xing";

            // Is "Xing" ?
            if (string.Equals(xing, Encoding.ASCII.GetString(tmp)))
            {
                this.h_vbr = true;
                this.h_vbr_frames = -1;
                this.h_vbr_bytes = -1;
                this.h_vbr_scale = -1;
                this.h_vbr_toc = new byte[100];

                int length = 4;

                // Read flags.
                var flags = new byte[4];
                Array.Copy(firstFrame, offset + length, flags, 0, flags.Length);
                length += flags.Length;

                // Read number of frames (if available).
                if ((flags[3] & (1 << 0)) != 0)
                {
                    Array.Copy(firstFrame, offset + length, tmp, 0, tmp.Length);
                    this.h_vbr_frames = (int) ((tmp[0] << 24) & 0xFF000000 |
                                               (tmp[1] << 16) & 0x00FF0000 |
                                               (tmp[2] << 8) & 0x0000FF00 |
                                                tmp[3] & 0x000000FF);
                    length += 4;
                }

                // Read size (if available).
                if ((flags[3] & (1 << 1)) != 0)
                {
                    Array.Copy(firstFrame, offset + length, tmp, 0, tmp.Length);
                    this.h_vbr_bytes = (int) ((tmp[0] << 24) & 0xFF000000 |
                                              (tmp[1] << 16) & 0x00FF0000 |
                                              (tmp[2] << 8) & 0x0000FF00 |
                                               tmp[3] & 0x000000FF);
                    length += 4;
                }

                // Read TOC (if available).
                if ((flags[3] & (1 << 2)) != 0)
                {
                    Array.Copy(firstFrame, offset + length, this.h_vbr_toc, 0, this.h_vbr_toc.Length);
                    length += this.h_vbr_toc.Length;
                }

                // Read scale (if available).
                if ((flags[3] & (1 << 3)) != 0)
                {
                    Array.Copy(firstFrame, offset + length, tmp, 0, tmp.Length);
                    this.h_vbr_scale = (int) ((tmp[0] << 24) & 0xFF000000 |
                                              (tmp[1] << 16) & 0x00FF0000 |
                                              (tmp[2] << 8) & 0x0000FF00 |
                                               tmp[3] & 0x000000FF);
                }
            }
        }

        /// <summary>
        ///     Returns Sample Frequency
        /// </summary>
        public int SampleFrequency()
        {
            return this.h_sample_frequency;
        }

        /// <summary>
        ///     Returns Sample Frequency in Hertz
        /// </summary>
        public int SampleFrequencyHertz()
        {
           switch (this.h_sample_frequency)
            {
                case ThirtyTwo:
                    switch (this.h_version)
                    {
                        case Mpeg1:
                            return 32000;
                        case MPEG2_LSF:
                            return 16000;
                        default:
                            return 8000;
                    }

                case FourtyFourPointOne:
                    switch (this.h_version)
                    {
                        case Mpeg1:
                            return 44100;
                        case MPEG2_LSF:
                            return 22050;
                        default:
                            return 11025;
                    }

                case FourtyEight:
                    switch (this.h_version)
                    {
                        case Mpeg1:
                            return 48000;
                        case MPEG2_LSF:
                            return 24000;
                        default:
                            return 12000;
                    }
                    break;
            }
            return 0;
        }

        /// <summary>
        ///     Returns Frequency
        /// </summary>
        public string SampleFrequencyString()
        {
            switch (this.h_sample_frequency)
            {
                case ThirtyTwo:
                    switch (this.h_version)
                    {
                        case Mpeg1:
                            return "32 kHz";
                        case MPEG2_LSF:
                            return "16 kHz";
                        default:
                            return "8 kHz";
                    }

                case FourtyFourPointOne:
                    switch (this.h_version)
                    {
                        case Mpeg1:
                            return "44.1 kHz";
                        case MPEG2_LSF:
                            return "22.05 kHz";
                        default:
                            return "11.025 kHz";
                    }

                case FourtyEight:
                    switch (this.h_version)
                    {
                        case Mpeg1:
                            return "48 kHz";
                        case MPEG2_LSF:
                            return "24 kHz";
                        default:
                            return "12 kHz";
                    }
                    break;
            }
            return (null);
        }
        
        /// <summary>
        ///     Returns Slots.
        /// </summary>
        public int Slots()
        {
            return this.nSlots;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder(200);
            buffer.Append("Layer ");
            buffer.Append(this.LayerString());
            buffer.Append(" frame ");
            buffer.Append(this.ModeString());
            buffer.Append(' ');
            buffer.Append(this.VersionString());
            if (!this.CheckSums())
            {
                buffer.Append(" no");
            }
            buffer.Append(" checksums");
            buffer.Append(' ');
            buffer.Append(this.SampleFrequencyString());
            buffer.Append(',');
            buffer.Append(' ');
            buffer.Append(this.BitrateString());

            return buffer.ToString();
        }

        /// <summary>
        ///     Returns total ms.
        /// </summary>
        public float TotalSeconds(int streamsize)
        {
            if (this.h_vbr_frames != 0)
            {
                return (h_vbr_frames*this.MsPerFrame()) / 1000;
            }

            return (float)(streamsize * 8) / this.Bitrate();
        }

        /// <summary>
        ///     Returns version.
        /// </summary>
        public int Version()
        {
            return this.h_version;
        }

        /// <summary>
        ///     Returns Version.
        /// </summary>
        public string VersionString()
        {
            switch (this.h_version)
            {
                case Mpeg1:
                    return "MPEG-1";

                case MPEG2_LSF:
                    return "MPEG-2 LSF";

                case MPEG25_LSF:
                    return "MPEG-2.5 LSF";
            }
            return (null);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Read a 32-bit header from the bitstream.
        /// </summary>
        internal void ReadHeader(Bitstream stream, Crc16[] crcp)
        {
            int headerstring;
            int channel_bitrate;

            bool sync = false;

            do
            {
                IList<Id3V2Frame> metadata = stream.ReadMetadata();
                if (metadata != null)
                {
                    this.metadata = metadata;
                }

                headerstring = stream.SyncHeader(this.syncmode);
                this._headerstring = headerstring; // E.B

                if (this.syncmode == Bitstream.INITIAL_SYNC)
                {
                    this.h_version = headerstring >> 11;
                    this.h_version = ((SupportClass.UrShift(headerstring, 19)) & 1);
                    if (((SupportClass.UrShift(headerstring, 20)) & 1) == 0)
                    {
                        // SZD: MPEG2.5 detection
                        if (this.h_version == MPEG2_LSF)
                        {
                            this.h_version = MPEG25_LSF;
                        }
                        else
                        {
                            throw new BitstreamException(BitstreamErrorsFields.UnknownError, null);
                        }
                    }

                    if ((this.h_sample_frequency = ((SupportClass.UrShift(headerstring, 10)) & 3)) == 3)
                    {
                        throw new BitstreamException(BitstreamErrorsFields.UnknownError, null);
                    }
                }

                this.h_layer = 4 - (SupportClass.UrShift(headerstring, 17)) & 3;
                if (this.h_layer == 4)
                {
                    continue;
                }

                this.h_protection_bit = (SupportClass.UrShift(headerstring, 16)) & 1;

                this.h_bitrate_index = (SupportClass.UrShift(headerstring, 12)) & 0xF;
                if (this.h_bitrate_index == 15)
                {
                    continue;
                }

                this.h_padding_bit = (SupportClass.UrShift(headerstring, 9)) & 1;
                this.h_mode = ((SupportClass.UrShift(headerstring, 6)) & 3);
                this.h_mode_extension = (SupportClass.UrShift(headerstring, 4)) & 3;
                
                if (this.h_mode == JointStereo)
                {
                    this.h_intensity_stereo_bound = (this.h_mode_extension << 2) + 4;
                }
                else
                {
                    this.h_intensity_stereo_bound = 0;
                }

                // should never be used
                if (((SupportClass.UrShift(headerstring, 3)) & 1) == 1)
                {
                    this.h_copyright = true;
                }
                
                if (((SupportClass.UrShift(headerstring, 2)) & 1) == 1)
                {
                    this.h_original = true;
                }

                // calculate number of subbands:
                if (this.h_layer == 1)
                {
                    this.h_number_of_subbands = 32;
                }
                else
                {
                    channel_bitrate = this.h_bitrate_index;

                    // calculate bitrate per channel:
                    if (this.h_mode != SingleChannel)
                    {
                        if (channel_bitrate == 4)
                        {
                            channel_bitrate = 1;
                        }
                        else
                        {
                            channel_bitrate -= 4;
                        }
                    }

                    if ((channel_bitrate == 1) || (channel_bitrate == 2))
                    {
                        this.h_number_of_subbands = this.h_sample_frequency == ThirtyTwo ? 12 : 8;
                    }
                    else if ((this.h_sample_frequency == FourtyEight)
                             || ((channel_bitrate >= 3) && (channel_bitrate <= 5)))
                    {
                        this.h_number_of_subbands = 27;
                    }
                    else
                    {
                        this.h_number_of_subbands = 30;
                    }
                }
                if (this.h_intensity_stereo_bound > this.h_number_of_subbands)
                {
                    this.h_intensity_stereo_bound = this.h_number_of_subbands;
                }

                // calculate framesize and nSlots
                this.CalculateFrameSize();

                // read framedata:
                int frameSizeLoaded = stream.ReadFrameData(this.framesize);

                if ((this.framesize >= 0) && (frameSizeLoaded != this.framesize))
                {
                    throw new BitstreamException(BitstreamErrorsFields.InvalidFrame, null);
                }

                if (stream.IsSyncCurrentPosition(this.syncmode))
                {
                    if (this.syncmode == Bitstream.INITIAL_SYNC)
                    {
                        this.syncmode = Bitstream.STRICT_SYNC;
                        stream.SetSyncword(headerstring & unchecked((int)0xFFF80CC0));
                    }
                    sync = true;
                }
                else
                {
                    stream.UnreadFrame();
                }
            }
            while (!sync);

            stream.ParseFrame();

            if (this.h_protection_bit == 0)
            {
                // frame contains a crc checksum
                this.checksum = (short)stream.GetBits(16);
                if (this.crc == null)
                {
                    this.crc = new Crc16();
                }

                this.crc.AddBits(headerstring, 16);
                crcp[0] = this.crc;
            }
            else
            {
                crcp[0] = null;
            }

            if (this.h_sample_frequency == FourtyFourPointOne)
            {
                /*
                if (offset == null)
                {
                int max = max_number_of_frames(stream);
                offset = new int[max];
                for(int i=0; i<max; i++) offset[i] = 0;
                }
                // Bizarre, y avait ici une acollade ouvrante
                int cf = stream.current_frame();
                int lf = stream.last_frame();
                if ((cf > 0) && (cf == lf))
                {
                offset[cf] = offset[cf-1] + h_padding_bit;
                }
                else
                {
                offset[0] = h_padding_bit;
                }
                */
            }
        }

        private void InitBlock()
        {
            this.syncmode = Bitstream.INITIAL_SYNC;
        }

        #endregion

        // E.B
    }
}