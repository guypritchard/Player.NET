/*
* 02/12/99 : Java Conversion by E.B , ebsp@iname.com, JavaLayer
*
*-----------------------------------------------------------------------
*  @(#) crc.h 1.5, last edit: 6/15/94 16:55:32
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
*-----------------------------------------------------------------------
*/

namespace javazoom.jl.decoder
{
    using Support;

    /// <summary>
    ///     16-Bit CRC checksum
    /// </summary>
    public sealed class Crc16
    {
        #region Constants

        private const short Polynomial = -32763;

        #endregion

        #region Fields

        private short crc;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public Crc16()
        {
            this.crc = short.MaxValue;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Feed a bitstring to the crc calculation
        /// </summary>
        public void AddBits(int bitstring, int length)
        {
            int bitmask = 1 << (length - 1);

            do
            {
                if (((this.crc & 0x8000) == 0) ^ ((bitstring & bitmask) == 0))
                {
                    this.crc <<= 1;
                    this.crc ^= Polynomial;
                }
                else
                {
                    this.crc <<= 1;
                }
            }
            while ((bitmask = SupportClass.UrShift(bitmask, 1)) != 0);
        }

        /// <summary>
        ///     Return the calculated checksum.
        ///     Erase it for next calls to AddBits().
        /// </summary>
        public short Checksum()
        {
            short sum = this.crc;
            this.crc = short.MaxValue;
            return sum;
        }

        #endregion
    }
}