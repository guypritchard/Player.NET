namespace DJPad.Input.Wave
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using DJPad.Formats.Wave;

    /// <summary>
    ///     This class gives you repurposable read/write access to a wave file.
    /// </summary>
    public class WaveFileStream : Stream
    {
        #region Fields

        private readonly BinaryReader reader;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Creates a new stream instance using the provided filename, and the default chunk size of 4096 bytes.
        /// </summary>
        public WaveFileStream(string fileName)
            : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
        }

        /// <summary>
        ///     Creates a new stream instance using the provided stream as a source, and the default chunk size of 4096 bytes.
        /// </summary>
        public WaveFileStream(Stream sourceStream)
        {
            this.reader = new BinaryReader(sourceStream);
        }

        #endregion

        #region Public Properties

        public override bool CanRead
        {
            get
            {
                return this.reader.BaseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.reader.BaseStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public DataChunk Data { get; set; }

        public FactChunk Fact { get; set; }

        public FmtChunk Format { get; set; }

        public override long Length
        {
            get
            {
                return this.reader.BaseStream.Length;
            }
        }

        public RiffChunk Mainfile { get; set; }

        public override long Position
        {
            get
            {
                return this.reader.BaseStream.Position;
            }
            set
            {
                this.reader.BaseStream.Position = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void AdvanceToNext()
        {
            long NextOffset = this.reader.ReadUInt32(); //Get next chunk offset
            //Seek to the next offset from current position
            this.reader.BaseStream.Seek(NextOffset, SeekOrigin.Current);
        }

        public new void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Close();
            }
        }

        public override void Flush()
        {
            this.reader.BaseStream.Flush();
        }

        /// <summary>
        ///     Return the type of the current chunk - must be at the point where a chunk is expected.
        /// </summary>
        /// <returns>A ChunkType enumeration value.</returns>
        public ChunkType GetChunkName()
        {
            var chars = this.reader.ReadChars(4);
            var chunkType = new string(chars);

            if (string.IsNullOrEmpty(chunkType))
            {
                throw new InvalidDataException("No chunk type returned.");
            }

            switch (chunkType)
            {
                case "fmt ":
                    return ChunkType.Fmt;

                case "fact":
                    return ChunkType.Fact;

                case "data":
                    return ChunkType.Data;

                case "RIFF":
                    return ChunkType.Riff;

                default:
                    Debug.WriteLine("Unknown chunk:" + chunkType);
                    return ChunkType.Unknown;
            }
        }

        public void ParseHeadersAndPrepareToRead()
        {
            while (this.Data == null || this.Format == null)
            {
                ChunkType chunkType = this.GetChunkName();

                switch (chunkType)
                {
                    case ChunkType.Fmt:
                        this.Format = this.ReadFormatHeader();
                        break;

                    case ChunkType.Data:
                        this.Data = this.ReadDataHeader();
                        break;

                    case ChunkType.Riff:
                        this.Mainfile = this.ReadMainFileHeader();
                        break;

                    case ChunkType.Fact:
                        this.Fact = this.ReadFactHeader();
                        break;

                    default:
                        // this.AdvanceToNext();
                        break;
                }
            }

            this.reader.BaseStream.Seek(this.Data.lFilePosition, SeekOrigin.Begin);
        }

        public byte[] Read(int size)
        {
            return this.reader.ReadBytes(size);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.reader.BaseStream.Read(buffer, offset, count);
        }

        public DataChunk ReadDataHeader()
        {
            this.Data = new DataChunk();

            this.Data.sChunkID = "data";
            this.Data.dwChunkSize = this.reader.ReadUInt32();
            this.Data.lFilePosition = this.reader.BaseStream.Position;
            if (!(this.Fact == null))
            {
                this.Data.dwNumSamples = this.Fact.dwNumSamples;
            }
            else
            {
                this.Data.dwNumSamples = this.Data.dwChunkSize
                                         / ((uint)this.Format.dwBitsPerSample / 8 * this.Format.wChannels);
            }
            //The above could be written as data.dwChunkSize / format.wBlockAlign, but I want to emphasize what the frames look like.
            this.Data.dwMinLength = (this.Data.dwChunkSize / this.Format.dwAvgBytesPerSec) / 60;
            this.Data.dSecLength = this.Data.dwNumSamples / (double)this.Format.dwSamplesPerSec;
            return this.Data;
        }

        public FactChunk ReadFactHeader()
        {
            this.Fact = new FactChunk();

            this.Fact.sChunkID = "fact";
            this.Fact.dwChunkSize = this.reader.ReadUInt32();
            this.Fact.dwNumSamples = this.reader.ReadUInt32();
            return this.Fact;
        }

        public FmtChunk ReadFormatHeader()
        {
            this.Format = new FmtChunk();

            this.Format.sChunkID = "fmt ";
            this.Format.dwChunkSize = this.reader.ReadUInt32();
            this.Format.wFormatTag = this.reader.ReadUInt16();
            this.Format.wChannels = this.reader.ReadUInt16();
            this.Format.dwSamplesPerSec = this.reader.ReadUInt32();
            this.Format.dwAvgBytesPerSec = this.reader.ReadUInt32();
            this.Format.wBlockAlign = this.reader.ReadUInt16();
            this.Format.dwBitsPerSample = this.reader.ReadUInt16();

            // Some format chunks have extra data.
            uint extraData = this.Format.dwChunkSize - 16;
            this.reader.ReadBytes((int)extraData);

            return this.Format;
        }

        public RiffChunk ReadMainFileHeader()
        {
            this.Mainfile = new RiffChunk();

            this.Mainfile.sGroupID = "RIFF";
            this.Mainfile.dwFileLength = this.reader.ReadUInt32() + 8;
            this.Mainfile.sRiffType = new string(this.reader.ReadChars(4));
            return this.Mainfile;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.reader.BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.reader.BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("WaveFileStream::Write");
        }

        #endregion
    }
}