///A BackStream (such a beast doesn't exist in C#'s libraries to my knowledge)

namespace javazoom.jl.decoder
{
    using System;
    using System.IO;
    using System.Text;

    [Serializable]
    internal class CircularByteBuffer
    {
        #region Fields

        private byte[] dataArray;

        private int index;

        private int length = 1;

        private int numValid;

        #endregion

        #region Constructors and Destructors

        public CircularByteBuffer(int size)
        {
            this.dataArray = new byte[size];
            this.length = size;
        }

        /// <summary>
        ///     Initialize by copying the CircularByteBuffer passed in
        /// </summary>
        public CircularByteBuffer(CircularByteBuffer cdb)
        {
            lock (cdb)
            {
                this.length = cdb.length;
                this.numValid = cdb.numValid;
                this.index = cdb.index;
                this.dataArray = new byte[this.length];
                for (int c = 0; c < this.length; c++)
                {
                    this.dataArray[c] = cdb.dataArray[c];
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The physical size of the Buffer (read/write)
        /// </summary>
        public int BufferSize
        {
            get
            {
                return this.length;
            }
            set
            {
                var newDataArray = new byte[value];

                int minLength = (this.length > value) ? value : this.length;
                for (int i = 0; i < minLength; i++)
                {
                    newDataArray[i] = this.InternalGet(i - this.length + 1);
                }
                this.dataArray = newDataArray;
                this.index = minLength - 1;
                this.length = value;
            }
        }

        /// <summary>
        ///     How far back it is safe to look (read/write).  Write only to reduce NumValid.
        /// </summary>
        public int NumValid
        {
            get
            {
                return this.numValid;
            }
            set
            {
                if (value > this.numValid)
                {
                    throw new ArgumentException(
                        "Can't set NumValid to " + value + " which is greater than the current numValid value of "
                        + this.numValid);
                }

                this.numValid = value;
            }
        }

        #endregion

        #region Public Indexers

        /// <summary>
        ///     e.g. Offset[0] is the current value
        /// </summary>
        public byte this[int index]
        {
            get
            {
                return this.InternalGet(-1 - index);
            }
            set
            {
                this.InternalSet(-1 - index, value);
            }
        }

        #endregion

        #region Public Methods and Operators

        public CircularByteBuffer Copy()
        {
            return new CircularByteBuffer(this);
        }

        /// <summary>
        ///     Returns a range (in terms of Offsets) in an int array in chronological (oldest-to-newest) order. e.g. (3, 0)
        ///     returns the last four ints pushed, with result[3] being the most recent.
        /// </summary>
        public byte[] GetRange(int str, int stp)
        {
            var outByte = new byte[str - stp + 1];

            for (int i = str, j = 0; i >= stp; i--, j++)
            {
                outByte[j] = this[i];
            }

            return outByte;
        }

        /// <summary>
        ///     Returns what would fall out of the buffer on a Push.  NOT the same as what you'd get with a Pop().
        /// </summary>
        public byte Peek()
        {
            lock (this)
            {
                return this.InternalGet(this.length);
            }
        }

        /// <summary>
        ///     Pop an integer off the start of the buffer. Throws an exception if the buffer is empty (NumValid == 0)
        /// </summary>
        public byte Pop()
        {
            lock (this)
            {
                if (this.numValid == 0)
                {
                    throw new Exception("Can't pop off an empty CircularByteBuffer");
                }
                this.numValid--;
                return this[this.numValid];
            }
        }

        /// <summary>
        ///     Push a byte into the buffer.  Returns the value of whatever comes off.
        /// </summary>
        public byte Push(byte newValue)
        {
            byte ret;
            lock (this)
            {
                ret = this.InternalGet(this.length);
                this.dataArray[this.index] = newValue;
                this.numValid++;
                if (this.numValid > this.length)
                {
                    this.numValid = this.length;
                }
                this.index++;
                this.index %= this.length;
            }
            return ret;
        }

        public void Reset()
        {
            this.index = 0;
            this.numValid = 0;
        }

        public override string ToString()
        {
            var ret = new StringBuilder();

            for (int i = 0; i < this.dataArray.Length; i++)
            {
                ret.Append(this.dataArray[i] + " ");
            }

            ret.Append("\n index = " + this.index + " numValid = " + this.NumValid);
            return ret.ToString();
        }

        #endregion

        #region Methods

        private byte InternalGet(int offset)
        {
            int ind = this.index + offset;

            // Do thin modulo (should just drop through)
            for (; ind >= this.length; ind -= this.length)
            {
                ;
            }
            for (; ind < 0; ind += this.length)
            {
                ;
            }
            // Set value
            return this.dataArray[ind];
        }

        private void InternalSet(int offset, byte valueToSet)
        {
            int ind = this.index + offset;

            // Do thin modulo (should just drop through)
            for (; ind > this.length; ind -= this.length)
            {
                ;
            }
            for (; ind < 0; ind += this.length)
            {
                ;
            }
            // Set value
            this.dataArray[ind] = valueToSet;
        }

        #endregion
    }

    internal class BackStream
    {
        #region Fields

        private readonly int BackBufferSize;

        private readonly CircularByteBuffer COB;

        private readonly byte[] Temp;

        private readonly Stream stream;

        private int NumForwardBytesInBuffer;

        #endregion

        #region Constructors and Destructors

        public BackStream(Stream s, int backBufferSize)
        {
            this.stream = s;
            this.BackBufferSize = backBufferSize;
            this.Temp = new byte[this.BackBufferSize];
            this.COB = new CircularByteBuffer(this.BackBufferSize);
        }

        #endregion

        #region Public Methods and Operators

        public void Close()
        {
            this.stream.Close();
        }

        public int Read(byte[] toRead, int offset, int length)
        {
            // Read 
            int currentByte = 0;
            bool canReadStream = true;
            while (currentByte < length && canReadStream)
            {
                if (this.NumForwardBytesInBuffer > 0)
                {
                    // from mem
                    this.NumForwardBytesInBuffer--;
                    toRead[offset + currentByte] = this.COB[this.NumForwardBytesInBuffer];
                    currentByte++;
                }
                else
                {
                    // from stream
                    int newBytes = length - currentByte;
                    int numRead = this.stream.Read(this.Temp, 0, newBytes);
                    canReadStream = numRead >= newBytes;
                    for (int i = 0; i < numRead; i++)
                    {
                        this.COB.Push(this.Temp[i]);
                        toRead[offset + currentByte + i] = this.Temp[i];
                    }
                    currentByte += numRead;
                }
            }
            return currentByte;
        }

        /// <summary>
        ///     Unbuffered read (for embedded data)
        /// </summary>
        /// <param name="toRead"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public int ReadDirect(byte[] toRead, int offset, int length)
        {
            return this.stream.Read(toRead, offset, length);
        }

        public void Skip(int length)
        {
            this.stream.Seek(length, SeekOrigin.Current);
        }

        public void UnRead(int length)
        {
            this.NumForwardBytesInBuffer += length;
            if (this.NumForwardBytesInBuffer > this.BackBufferSize)
            {
                Console.WriteLine("YOUR BACKSTREAM IS BROKEN!");
            }
        }

        #endregion
    }
}