// $Id: Mp3Stream.cs,v 1.3 2004/08/03 16:20:37 tekhedd Exp $
//
// Fri Jul 30 20:39:30 EDT 2004
// Rewrote the buffer object to hold one frame at a time for 
// efficiency. Commented out some functions rather than taking
// the time to port them. --t/DD

// Rob, Sept 1:
// - Changed access for all classes in this project except Mp3Sharp and the Exceptions to internal 
// - Removed commenting from DecodeFrame method of Mp3Stream
// - Added GPL license to Mp3Sharp.cs
// - Changed version number to 1.4

// Guy Jan 1st 2013:
// - Tinkering with the code formatting.
// - Removing redundant code where found (redundant for Mp3 playback)

// Guy May 6th 2013
// - Added clunky ID3 support.

/*
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

namespace Mp3Sharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using ID3;

    using javazoom.jl.decoder;

    /// <summary>
    ///     Provides a view of the sequence of bytes that are produced during the conversion of an MP3 stream
    ///     into a 16-bit PCM-encoded ("WAV" format) stream.
    /// </summary>
    public class Mp3Stream : Stream
    {
        #region Fields

        /// <summary>
        ///     Used to interface with javaZoom.
        /// </summary>
        private readonly Bitstream JZBitStream;

        /// <summary>
        ///     Used to interface with javaZoom.
        /// </summary>
        private Decoder JZDecoder = new Decoder(new Decoder.Params());

        private OBuffer16BitStereo QueueOBuffer;

        private readonly Stream SourceStream;

        private int BackStreamByteCountRep;

        private short ChannelCountRep = -1;

        protected SoundFormat FormatRep = SoundFormat.Pcm16BitStereo;

        private int FrequencyRep = -1;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Creates a new stream instance using the provided filename, and the default chunk size of 4096 bytes.
        /// </summary>
        public Mp3Stream(string fileName)
            : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            this.FileName = fileName;
        }

        /// <summary>
        ///     Creates a new stream instance using the provided filename and chunk size.
        /// </summary>
        public Mp3Stream(string fileName, int chunkSize)
            : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read), chunkSize)
        {
            this.FileName = fileName;
        }

        /// <summary>
        ///     Creates a new stream instance using the provided stream as a source, and the default chunk size of 4096 bytes.
        /// </summary>
        public Mp3Stream(Stream sourceStream)
            : this(sourceStream, 4096)
        {
        }

        /// <summary>
        ///     Creates a new stream instance using the provided stream as a source.
        ///     TODO: allow selecting stereo or mono in the constructor (note that this also requires "implementing" the stereo format).
        /// </summary>
        public Mp3Stream(Stream sourceStream, int chunkSize)
        {
            this.FormatRep = SoundFormat.Pcm16BitStereo;
            this.SourceStream = sourceStream;
            this.JZBitStream = new Bitstream(new BackStream(this.SourceStream, chunkSize));
            this.QueueOBuffer = new OBuffer16BitStereo();
            this.JZDecoder.OutputBuffer = this.QueueOBuffer;
        }

        #endregion

        #region Public Properties

        public override bool CanRead
        {
            get
            {
                return this.SourceStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.SourceStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.SourceStream.CanWrite;
            }
        }

        /// <summary>
        ///     Gets the number of channels available in the audio being decoded.
        ///     Initially set to -1.  Initialized during the first call to either of the Read and DecodeFrames methods,
        ///     and updated during every subsequent call to one of those methods to reflect the most recent header information
        ///     from the MP3 stream.
        /// </summary>
        public short ChannelCount
        {
            get
            {
                return this.ChannelCountRep;
            }
        }

        public int ChunkSize
        {
            get
            {
                return this.BackStreamByteCountRep;
            }
        }

        /// <summary>
        ///     Gets or sets the PCM output format of this stream.
        /// </summary>
        public SoundFormat Format
        {
            get
            {
                return this.FormatRep;
            }

            // Note: the buffers are stored in an optimized format--changing
            // the Format involves flushing the buffers and so on, so 
            // let's just not, OK?
            // set { FormatRep = value; } 
        }

        /// <summary>
        ///     Gets the frequency of the audio being decoded.
        ///     Initially set to -1.  Initialized during the first call to either of the Read and DecodeFrames methods,
        ///     and updated during every subsequent call to one of those methods to reflect the most recent header information
        ///     from the MP3 stream.
        /// </summary>
        public int Frequency
        {
            get
            {
                return this.FrequencyRep;
            }
        }

        public IList<Id3V2Frame> Id3Frames { get; set; }

        public double DurationSeconds { get; private set; }

        public string FileName { get; private set; }

        public override long Length
        {
            get
            {
                return this.SourceStream.Length;
            }
        }

        /// <summary>
        ///     Gets or sets the position of the source stream.  This is relative to the number of bytes in the MP3 file, rather than
        ///     the total number of PCM bytes (typically significantly greater) contained in the Mp3Stream's output.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.SourceStream.Position;
            }
            set
            {
                this.SourceStream.Position = value;
            }
        }

        public long PlaybackStartPosition { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Closes the source stream and releases any associated resources.
        ///     If you don't call this, you may be leaking file descriptors.
        /// </summary>
        public override void Close()
        {
            this.JZBitStream.Close(); 
        }

        /// <summary>
        ///     Decodes the requested number of frames from the MP3 stream
        ///     and caches their PCM-encoded bytes.  These can subsequently be obtained using the Read method.
        ///     Returns the number of frames that were successfully decoded.
        /// </summary>
        public int DecodeFrames(int frameCount)
        {
            int framesDecoded = 0;
            bool aFrameWasRead = true;
            while (framesDecoded < frameCount && aFrameWasRead)
            {
                aFrameWasRead = this.ReadFrame();
                if (aFrameWasRead)
                {
                    framesDecoded++;
                }
            }
            return framesDecoded;
        }

        public override void Flush()
        {
            this.SourceStream.Flush();
        }

        public void PreRoll()
        {
            if (this.Id3Frames != null)
            {
                this.Id3Frames = null;
            }

            for (int i = 0; i < 3; i++)
            {
                if (this.Id3Frames == null)
                {
                    var succeeded = this.ReadFrame();
                    // We may not have any data so should stop.
                    if (!succeeded)
                    {
                        throw new InvalidOperationException("Unable to preroll file possibly invalid.");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        ///     Reads the MP3 stream as PCM-encoded bytes.  Decodes a portion of the stream if necessary.
        ///     Returns the number of bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Copy from queue buffers, reading new ones as necessary,
            // until we can't read more or we have read "count" bytes
            int bytesRead = 0;

            do
            {
                if (this.QueueOBuffer.bytesLeft <= 0)
                {
                    if (!this.ReadFrame()) // out of frames or end of stream?
                    {
                        break;
                    }
                }

                // Copy as much as we can from the current buffer:
                bytesRead += this.QueueOBuffer.Read(buffer, offset + bytesRead, count - bytesRead);

                if (bytesRead >= count)
                {
                    break;
                }
            }
            while (true);

            return bytesRead;
        }

        /// <summary>
        ///     Sets the position of the source stream.
        /// </summary>
        public override long Seek(long pos, SeekOrigin origin)
        {
            this.JZBitStream.CloseFrame();      
            this.JZDecoder = new Decoder(new Decoder.Params());
            this.JZDecoder.OutputBuffer = this.QueueOBuffer;
            
            var position = this.SourceStream.Seek(pos, origin);
            return position;
        }

        /// <summary>
        ///     This method is not valid for an Mp3Stream.
        /// </summary>
        public override void SetLength(long len)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     This method is not valid for an Mp3Stream.
        /// </summary>
        public override void Write(byte[] buf, int ofs, int count)
        {
            throw new InvalidOperationException();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Reads a frame from the MP3 stream.  Returns whether the operation was successful.  If it wasn't,
        ///     the source stream is probably at its end.
        /// </summary>
        private bool ReadFrame()
        {
            // Read a frame from the bitstream.
            Header header = this.JZBitStream.ReadFrame();
            if (header == null)
            {
                return false;
            }

            // Trace.WriteLine(string.Format("Before decode {0}", this.Position));

            try
            {
                // Read Id3 tags in header if any.
                this.Id3Frames = header.Metadata();

                if (this.DurationSeconds.Equals(0.0d))
                {
                    this.PlaybackStartPosition = this.Position;
                    this.DurationSeconds = header.TotalSeconds((int) (this.Length - this.Position));
                }

                // Set the channel count and frequency values for the stream.
                this.ChannelCountRep = header.Mode() == Header.SingleChannel ? (short) 1 : (short) 2;

                this.FrequencyRep = header.Frequency();

                // Decode the frame.
                Obuffer decoderOutput = this.JZDecoder.DecodeFrame(header, this.JZBitStream);

                // Trace.WriteLine(string.Format("After decode {0}", this.Position));

                // Apparently, the way JavaZoom sets the output buffer 
                // on the decoder is a bit dodgy. Even though
                // this exception should never happen, we test to be sure.
                if (decoderOutput != this.QueueOBuffer)
                {
                    throw new ApplicationException("Output buffers are different.");
                }
            }
            catch (BitstreamException bitstreamException)
            {
            }
            catch (DecoderException decoderException)
            {
                if (decoderException.ErrorCode == DecoderErrors.IllegalSubBandAllocation)
                {
                    return false;
                }
                throw;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                // No resource leaks please!
                this.JZBitStream.CloseFrame();
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    ///     Describes sound formats that can be produced by the Mp3Stream class.
    /// </summary>
    public enum SoundFormat
    {
        /// <summary>
        ///     PCM encoded, 16-bit Mono sound format.
        /// </summary>
        Pcm16BitMono,

        /// <summary>
        ///     PCM encoded, 16-bit Stereo sound format.
        /// </summary>
        Pcm16BitStereo,
    }
}