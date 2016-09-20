/*
* 12/12/99	 Based on Ibitstream. Exceptions thrown on errors,
*			 Tempoarily removed seek functionality. mdm@techie.com
*
* 02/12/99 : Java Conversion by E.B , ebsp@iname.com , JavaLayer
*
*----------------------------------------------------------------------
*  @(#) ibitstream.h 1.5, last edit: 6/15/94 16:55:34
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
*
*  Changes made by Jeff Tsay :
*  04/14/97 : Added function prototypes for new syncing and seeking
*  mechanisms. Also made this file portable.
*-----------------------------------------------------------------------
*/

using System.Threading;

namespace javazoom.jl.decoder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using ID3;

    using Mp3Sharp;

    using Support;

    /// <summary>
    ///     The <code>Bitstream</code> class is responsible for parsing
    ///     an MPEG audio bitstream.
    ///     <b>REVIEW:</b> much of the parsing currently occurs in the
    ///     various decoders. This should be moved into this class and associated
    ///     inner classes.
    /// </summary>
    public sealed class Bitstream
    {
        #region Constants

        /// <summary>
        ///     Maximum size of the frame buffer.
        /// </summary>
        private const int BUFFER_INT_SIZE = 433;

        #endregion

        #region Static Fields

        /// <summary>
        ///     Syncrhronization control constant for the initial
        ///     synchronization to the start of a frame.
        /// </summary>
        internal static sbyte INITIAL_SYNC = 0;

        /// <summary>
        ///     Syncrhronization control constant for non-iniital frame
        ///     synchronizations.
        /// </summary>
        internal static sbyte STRICT_SYNC = 1;

        #endregion

        #region Fields

        private readonly int[] bitmask = new[]
                                             {
                                                 0x00000000, 0x00000001, 0x00000003, 0x00000007, 0x0000000F, 0x0000001F,
                                                 0x0000003F, 0x0000007F, 0x000000FF, 0x000001FF, 0x000003FF, 0x000007FF,
                                                 0x00000FFF, 0x00001FFF, 0x00003FFF, 0x00007FFF, 0x0000FFFF, 0x0001FFFF
                                             };

        private readonly BackStream source;

        /// <summary>
        ///     Number (0-31, from MSB to LSB) of next bit for get_bits()
        /// </summary>
        private int bitindex;

        private Crc16[] crc;

        private bool firstFrame = true;

        /// <summary>
        ///     The bytes read from the stream.
        /// </summary>
        private byte[] frame_bytes;

        /// <summary>
        ///     The frame buffer that holds the data for the current frame.
        /// </summary>
        private int[] framebuffer;

        /// <summary>
        ///     Number of valid bytes in the frame buffer.
        /// </summary>
        private int framesize;

        private Header header;

        private IList<Id3V2Frame> id3Tags;

        /// <summary>
        ///     *
        /// </summary>
        private bool single_ch_mode;

        private byte[] syncbuf;

        /// <summary>
        ///     The current specified syncword
        /// </summary>
        private int syncword;

        /// <summary>
        ///     Index into <code>framebuffer</code> where the next bits are
        ///     retrieved.
        /// </summary>
        private int wordpointer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Construct a IBitstream that reads data from a
        ///     given InputStream.
        /// </summary>
        internal Bitstream(BackStream in_Renamed)
        {
            this.firstFrame = true;

            this.InitBlock();

            if (in_Renamed == null)
            {
                throw new ArgumentNullException("in_Renamed");
            }

            this.source = in_Renamed;

            this.CloseFrame();
        }

        #endregion

        #region Public Methods and Operators

        public void Close()
        {
            try
            {
                this.source.Close();
            }
            catch (IOException ex)
            {
                throw new BitstreamException(BitstreamErrorsFields.StreamError, ex);
            }
        }

        public void CloseFrame()
        {
            this.framesize = - 1;
            this.wordpointer = - 1;
            this.bitindex = - 1;
        }

        /// <summary>
        ///     Read bits from buffer into the lower bits of an unsigned int.
        ///     The LSB contains the latest read bit of the stream.
        ///     (1 &lt= number_of_bits &lt= 16)
        /// </summary>
        public int GetBits(int number_of_bits)
        {
            int returnvalue = 0;
            int sum = this.bitindex + number_of_bits;

            // E.B
            // There is a problem here, wordpointer could be -1 ?!
            if (this.wordpointer < 0)
            {
                this.wordpointer = 0;
            }

            if (sum <= 32)
            {
                // all bits contained in *wordpointer
                returnvalue = (SupportClass.UrShift(this.framebuffer[this.wordpointer], (32 - sum)))
                              & this.bitmask[number_of_bits];
                // returnvalue = (wordpointer[0] >> (32 - sum)) & bitmask[number_of_bits];
                if ((this.bitindex += number_of_bits) == 32)
                {
                    this.bitindex = 0;
                    this.wordpointer++; // added by me!
                }
                return returnvalue;
            }

            int right = (this.framebuffer[this.wordpointer] & 0x0000FFFF);
            this.wordpointer++;
            var left = (int)(this.framebuffer[this.wordpointer] & 0xFFFF0000);
            returnvalue = (int)((right << 16) & 0xFFFF0000) | ((SupportClass.UrShift(left, 16)) & 0x0000FFFF);

            returnvalue = SupportClass.UrShift(returnvalue, 48 - sum);

            // returnvalue >>= 16 - (number_of_bits - (32 - bitindex))
            returnvalue &= this.bitmask[number_of_bits];
            this.bitindex = sum - 32;
            return returnvalue;
        }

        /// <summary>
        ///     Determines if the next 4 bytes of the stream represent a
        ///     frame header.
        /// </summary>
        public bool IsSyncCurrentPosition(int syncmode)
        {
            int read = this.ReadBytes(this.syncbuf, 0, 4);
            int headerstring = (int)((this.syncbuf[0] << 24) & 0xFF000000) | 
                                    ((this.syncbuf[1] << 16) & 0x00FF0000) | 
                                    ((this.syncbuf[2] << 8) & 0x0000FF00)  | 
                                    ((this.syncbuf[3] << 0) & 0x000000FF);

            try
            {
                this.source.UnRead(read);
            }
            catch (IOException ex)
            {
            }

            bool sync = false;
            switch (read)
            {
                case 0:
                    Trace.WriteLine("0 bytes read == sync?", "Bitstream");
                    sync = true;
                    break;

                case 4:
                    sync = this.isSyncMark(headerstring, syncmode, this.syncword);
                    break;
            }

            return sync;
        }

        /// <summary>
        ///     Unreads the bytes read from the frame.
        ///     @throws BitstreamException
        /// </summary>
        // REVIEW: add new error codes for this.
        public void UnreadFrame()
        {
            if (this.wordpointer == - 1 && this.bitindex == - 1 && (this.framesize > 0))
            {
                try
                {
                    this.source.UnRead(this.framesize);
                }
                catch (IOException ex)
                {
                    throw new BitstreamException(BitstreamErrorsFields.StreamError, ex);
                }
            }
        }

        public bool isSyncMark(int headerstring, int syncmode, int word)
        {
            bool sync = false;

            if (syncmode == INITIAL_SYNC)
            {
                sync = ((headerstring & 0xFFE00000) == 0xFFE00000); // SZD: MPEG 2.5
            }
            else
            {
                //sync = ((headerstring & 0xFFF80C00) == word) 
                sync = ((headerstring & 0xFFE00000) == 0xFFE00000)
                       && (((headerstring & 0x000000C0) == 0x000000C0) == this.single_ch_mode);
            }

            // filter out invalid sample rate
            if (sync)
            {
                sync = (((SupportClass.UrShift(headerstring, 10)) & 3) != 3);
                if (!sync)
                {
                    Debug.WriteLine("INVALID SAMPLE RATE DETECTED", "Bitstream");
                }
            }

            // filter out invalid layer
            if (sync)
            {
                sync = (((SupportClass.UrShift(headerstring, 17)) & 3) != 0);
                if (!sync)
                {
                    Debug.WriteLine("INVALID LAYER DETECTED", "Bitstream");
                }
            }

            // filter out invalid version
            if (sync)
            {
                sync = (((SupportClass.UrShift(headerstring, 19)) & 3) != 1);
                if (!sync)
                {
                    Debug.WriteLine("INVALID VERSION DETECTED");
                }
            }

            return sync;
        }

        // REVIEW: this class should provide inner classes to
        // parse the frame contents. Eventually, readBits will
        // be removed.
        public int readBits(int n)
        {
            return this.GetBits(n);
        }

        public int readCheckedBits(int n)
        {
            // REVIEW: implement CRC check.
            return this.GetBits(n);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Parses the data previously read with read_frame_data().
        /// </summary>
        internal void ParseFrame()
        {
            // Convert Bytes read to int
            int b = 0;
            byte[] byteread = this.frame_bytes;
            int bytesize = this.framesize;

            for (int k = 0; k < bytesize; k = k + 4)
            {
                int convert = 0;
                byte b0 = byteread[k];
                byte b1 = 0;
                byte b2 = 0;
                byte b3 = 0;

                if (k + 1 < bytesize)
                {
                    b1 = byteread[k + 1];
                }

                if (k + 2 < bytesize)
                {
                    b2 = byteread[k + 2];
                }

                if (k + 3 < bytesize)
                {
                    b3 = byteread[k + 3];
                }

                this.framebuffer[b++] = (int)((b0 << 24) & 0xFF000000) | ((b1 << 16) & 0x00FF0000)
                                        | ((b2 << 8) & 0x0000FF00) | (b3 & 0x000000FF);
            }

            this.wordpointer = 0;
            this.bitindex = 0;
        }

        /// <summary>
        ///     Reads and parses the next frame from the input source.
        /// </summary>
        /// <returns>
        ///     the Header describing details of the frame read,
        ///     or null if the end of the stream has been reached.
        /// </returns>
        internal Header ReadFrame()
        {
            Header result = null;
            try
            {
                result = this.ReadNextFrame();

                if (this.firstFrame)
                {
                    result.ParseVbr(this.frame_bytes);
                    this.firstFrame = false;
                }
            }
            catch (BitstreamException ex)
            {
                if (ex.ErrorCode == BitstreamErrorsFields.InvalidFrame)
                {
                    return this.ReadFrame();
                }

                if (ex.ErrorCode != BitstreamErrorsFields.StreamEof)
                {
                    // wrap original exception so stack trace is maintained.
                    throw new BitstreamException(ex.ErrorCode, ex);
                }
            }

            return result;
        }

        /// <summary>
        ///     Reads the data for the next frame. The frame is not parsed
        ///     until parse frame is called.
        /// </summary>
        internal int ReadFrameData(int bytesize)
        {
            int numread = 0;

            int bytesRead = this.ReadFully(this.frame_bytes, 0, bytesize);
            this.framesize = bytesize;
            this.wordpointer = - 1;
            this.bitindex = - 1;

            return bytesRead;
        }

        internal IList<Id3V2Frame>ReadMetadata()
        {
            // read additional 3 bytes
            if (this.ReadBytes(this.syncbuf, 0, 3) != 3)
            {
                throw new BitstreamException(BitstreamErrorsFields.StreamEof, null);
            }

            if (Id3V2Tag.IsID3(this.syncbuf))
            {
                var buf = new byte[Id3V2Tag.SizeInBytes];
                this.ReadBytes(buf, 0, buf.Length);

                Id3V2Tag tag = Id3V2Tag.ReadTagHeader(buf);
                
                var id3Data = new byte[tag.encodedSize - buf.Length];

                if (this.ReadDirect(id3Data, 0, id3Data.Length) != id3Data.Length)
                {
                    throw new BitstreamException(BitstreamErrorsFields.StreamEof, null);
                }

                if (tag.version[0] != 0x3)
                {
                    return new List<Id3V2Frame>();
                }

                return tag.ReadTagData(id3Data);
            }

            return null;
        }

        /// <summary>
        ///     Set the word we want to sync the header to.
        ///     In Big-Endian byte order
        /// </summary>
        internal void SetSyncword(int syncword0)
        {
            this.syncword = syncword0 & unchecked((int)0xFFFFFF3F);
            this.single_ch_mode = ((syncword0 & 0x000000C0) == 0x000000C0);
        }

        /// <summary>
        ///     Get next 32 bits from bitstream.
        ///     They are stored in the headerstring.
        ///     syncmod allows Synchro flag ID
        ///     The returned value is False at the end of stream.
        /// </summary>
        internal int SyncHeader(sbyte syncmode)
        {
            bool sync;
            int headerstring = ((this.syncbuf[0] << 16) & 0x00FF0000) | 
                               ((this.syncbuf[1] << 8) & 0x0000FF00)  | 
                               ((this.syncbuf[2] << 0) & 0x000000FF);

#if THROW_ON_SYNC_LOSS
    // t/DD: If we don't resync in a reasonable amount of time, 
    //       throw an exception
                        int bytesSkipped = 0;
                        bool lostSyncYet = false;
#endif
            do
            {
                headerstring <<= 8;

                if (this.ReadBytes(this.syncbuf, 3, 1) != 1)
                {
                    throw new BitstreamException(BitstreamErrorsFields.StreamEof, null);
                }

                headerstring |= (this.syncbuf[3] & 0x000000FF);
                sync = this.isSyncMark(headerstring, syncmode, this.syncword);

#if THROW_ON_SYNC_LOSS
    // Just for debugging -- if we lost sync, bitch
                                if (!sync && !lostSyncYet)
                                {
                                   lostSyncYet = true;
                                   Trace.WriteLine( "Lost Sync :(", "Bitstream" );
                                }

                                if (lostSyncYet && sync)
                                {
                                   Trace.WriteLine( "Found Sync", "Bitstream" );
                                }


                                // If we haven't resynced within a frame (or so) give up and
                                // throw an exception. (Could try harder?)
                                ++ bytesSkipped;
                                if ((bytesSkipped % 2048) == 0) // A paranoia check -- is the code hanging in a loop here?
                                {
                                    Trace.WriteLine( "Sync still not found", "Bitstream" );
                                    // throw newBitstreamException(javazoom.jl.decoder.BitstreamErrors_Fields.STREAM_ERROR, 
                                    //                             null);
                                }
#endif
            }
            while (!sync);

            return headerstring;
        }

        private void InitBlock()
        {
            this.crc = new Crc16[1];
            this.syncbuf = new byte[4];
            this.frame_bytes = new byte[BUFFER_INT_SIZE * 4];
            this.framebuffer = new int[BUFFER_INT_SIZE];
            this.header = new Header();
        }

        /// <summary>
        ///     *
        /// </summary>
        private void NextFrame()
        {
            // entire frame is read by the header class.
            this.header.ReadHeader(this, this.crc);
        }

        /// <summary>
        ///     Similar to readFully, but doesn't throw exception when
        ///     EOF is reached.
        /// </summary>
        private int ReadBytes(byte[] b, int offs, int len)
        {
            int totalBytesRead = 0;
            try
            {
                while (len > 0)
                {
                    int bytesread = this.source.Read(b, offs, len);

                    // for (int i = 0; i < len; i++) b[i] = (sbyte)Temp[i];

                    if (bytesread == - 1 || bytesread == 0)
                    {
                        break;
                    }
                    totalBytesRead += bytesread;
                    offs += bytesread;
                    len -= bytesread;
                }
            }
            catch (IOException ex)
            {
                throw new BitstreamException(BitstreamErrorsFields.StreamError, ex);
            }

            return totalBytesRead;
        }

        private int ReadDirect(byte[] b, int offs, int len)
        {
            return this.source.ReadDirect(b, offs, len);
        }

        /// <summary>
        ///     Reads the exact number of bytes from the source
        ///     input stream into a byte array.
        /// </summary>
        /// <param name="b">
        ///     byte array to read the specified number
        ///     of bytes into.
        /// </param>
        /// <param name="offs">
        ///     index in the array where the first byte
        ///     read should be stored.
        /// </param>
        /// <param name="len">
        ///     number of bytes to read.
        /// </param>
        /// <exception>
        ///     BitstreamException is thrown if the specified
        ///     number of bytes could not be read from the stream.
        /// </exception>
        private int ReadFully(byte[] b, int offs, int len)
        {
            int nRead = 0;
            try
            {
                while (len > 0)
                {
                    int bytesread = this.source.Read(b, offs, len);
                    if (bytesread == - 1 || bytesread == 0) // t/DD -- .NET returns 0 at end-of-stream!
                    {
                        // t/DD: this really SHOULD throw an exception here...
                        Debug.WriteLine("readFully -- returning success at EOF? (" + bytesread + ")", "Bitstream");
                        while (len-- > 0)
                        {
                            b[offs++] = 0;
                        }
                        break;
                        //throw newBitstreamException(UNEXPECTED_EOF, new EOFException());
                    }
                    nRead += bytesread;
                    offs += bytesread;
                    len -= bytesread;
                }
            }
            catch (IOException ex)
            {
                throw new BitstreamException(BitstreamErrorsFields.StreamError, ex);
            }

            return nRead;
        }

        private Header ReadNextFrame()
        {
            if (this.framesize == - 1)
            {
                this.NextFrame();
            }

            return this.header;
        }

        #endregion

        // max. 1730 bytes per frame: 144 * 384kbit/s / 32000 Hz + 2 Bytes CRC
    }
}