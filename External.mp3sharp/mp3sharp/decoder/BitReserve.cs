/*
* 12/12/99 0.0.7	Implementation stores single bits 
*					as ints for better performance. mdm@techie.com.
*
* Java Conversion by E.B, ebsp@iname.com, JavaLayer
*
*---------------------------------------------------
* bit_res.h
*
* 	Declarations for Bit Reservoir for Layer III
*
*  Adapted from the public c code by Jeff Tsay.
*---------------------------------------------------
*/

namespace javazoom.jl.decoder
{
    /// <summary>
    ///     Implementation of Bit Reservoir for Layer III.
    ///     The implementation stores single bits as a word in the buffer. If
    ///     a bit is set, the corresponding word in the buffer will be non-zero.
    ///     If a bit is clear, the corresponding word is zero. Although this
    ///     may seem waseful, this can be a factor of two quicker than
    ///     packing 8 bits to a byte and extracting.
    /// </summary>
    // REVIEW: there is no range checking, so buffer underflow or overflow
    // can silently occur.
    internal sealed class BitReserve
    {
        #region Constants

        /// <summary>
        ///     Size of the internal buffer to store the reserved bits.
        ///     Must be a power of 2. And x8, as each bit is stored as a single
        ///     entry.
        /// </summary>
        private const int Bufsize = 4096 * 8;

        /// <summary>
        ///     Mask that can be used to quickly implement the
        ///     modulus operation on BUFSIZE.
        /// </summary>
        private const int BufsizeMask = Bufsize - 1;

        #endregion

        #region Fields

        private int[] buf;

        private int buf_bit_idx;

        private int buf_byte_idx;

        private int offset, totbit;

        #endregion

        #region Constructors and Destructors

        internal BitReserve()
        {
            this.InitBlock();

            this.offset = 0;
            this.totbit = 0;
            this.buf_byte_idx = 0;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Rewind N bits in Stream.
        /// </summary>
        public void RewindNbits(int N)
        {
            this.totbit -= N;
            this.buf_byte_idx -= N;
            if (this.buf_byte_idx < 0)
            {
                this.buf_byte_idx += Bufsize;
            }
        }

        /// <summary>
        ///     Rewind N bytes in Stream.
        /// </summary>
        public void RewindNbytes(int N)
        {
            int bits = (N << 3);
            this.totbit -= bits;
            this.buf_byte_idx -= bits;
            if (this.buf_byte_idx < 0)
            {
                this.buf_byte_idx += Bufsize;
            }
        }

        /// <summary>
        ///     Returns next bit from reserve.
        /// </summary>
        /// <returns>
        ///     s 0 if next bit is reset, or 1 if next bit is set.
        /// </returns>
        public int hget1bit()
        {
            this.totbit++;
            int val = this.buf[this.buf_byte_idx];
            this.buf_byte_idx = (this.buf_byte_idx + 1) & BufsizeMask;
            return val;
        }

        /// <summary>
        ///     Read a number bits from the bit stream.
        /// </summary>
        /// <param name="N">
        ///     the number of
        /// </param>
        public int hgetbits(int N)
        {
            this.totbit += N;

            int val = 0;

            int pos = this.buf_byte_idx;
            if (pos + N < Bufsize)
            {
                while (N-- > 0)
                {
                    val <<= 1;
                    val |= ((this.buf[pos++] != 0) ? 1 : 0);
                }
            }
            else
            {
                while (N-- > 0)
                {
                    val <<= 1;
                    val |= ((this.buf[pos] != 0) ? 1 : 0);
                    pos = (pos + 1) & BufsizeMask;
                }
            }
            this.buf_byte_idx = pos;
            return val;
        }

        /// <summary>
        ///     Write 8 bits into the bit stream.
        /// </summary>
        public void hputbuf(int val)
        {
            int ofs = this.offset;
            this.buf[ofs++] = val & 0x80;
            this.buf[ofs++] = val & 0x40;
            this.buf[ofs++] = val & 0x20;
            this.buf[ofs++] = val & 0x10;
            this.buf[ofs++] = val & 0x08;
            this.buf[ofs++] = val & 0x04;
            this.buf[ofs++] = val & 0x02;
            this.buf[ofs++] = val & 0x01;

            if (ofs == Bufsize)
            {
                this.offset = 0;
            }
            else
            {
                this.offset = ofs;
            }
        }

        /// <summary>
        ///     Return totbit Field.
        /// </summary>
        public int hsstell()
        {
            return (this.totbit);
        }

        #endregion

        #region Methods

        private void InitBlock()
        {
            this.buf = new int[Bufsize];
        }

        #endregion
    }
}