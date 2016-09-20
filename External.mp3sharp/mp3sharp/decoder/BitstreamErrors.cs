/*
* 12/12/99		Initial version.	mdm@techie.com
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
    ///     This struct describes all error codes that can be thrown in <see cref="BistreamException" />
    /// </summary>
    /// <author>
    ///     MDM		12/12/99
    ///     @since	0.0.6
    /// </author>
    internal struct BitstreamErrorsFields
    {
        #region Static Fields

        public static readonly int BitstreamLast = 0x1ff;

        public static readonly int InvalidFrame;

        public static readonly int StreamEof;

        public static readonly int StreamError;

        public static readonly int UnexpectedEof;

        public static readonly int UnknownError;

        public static readonly int UnknownSampleRate;

        #endregion

        #region Constructors and Destructors

        static BitstreamErrorsFields()
        {
            UnknownError = GeneralErrors.BitstreamError + 0;
            UnknownSampleRate = GeneralErrors.BitstreamError + 1;
            StreamError = GeneralErrors.BitstreamError + 2;
            UnexpectedEof = GeneralErrors.BitstreamError + 3;
            StreamEof = GeneralErrors.BitstreamError + 4;
            InvalidFrame = GeneralErrors.BitstreamError + 5;
        }

        #endregion
    }
}