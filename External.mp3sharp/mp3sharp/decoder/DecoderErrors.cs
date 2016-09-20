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
    /// <summary>
    ///     This structure provides constants describing the error
    ///     codes used by the Decoder to indicate errors.
    /// </summary>
    /// <author>
    ///     MDM
    /// </author>
    internal struct DecoderErrors
    {
        #region Static Fields

        public static readonly int IllegalSubBandAllocation;

        public static readonly int UnknownError;

        public static readonly int UnsupportedLayer;

        #endregion

        #region Constructors and Destructors

        static DecoderErrors()
        {
            UnknownError = GeneralErrors.DecoderError + 0;
            UnsupportedLayer = GeneralErrors.DecoderError + 1;
            IllegalSubBandAllocation = GeneralErrors.DecoderError + 2;
        }

        #endregion
    }
}