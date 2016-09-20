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

    using Mp3Sharp;

    /// <summary>
    ///     The DecoderException represents the class of
    ///     errors that can occur when decoding MPEG audio.
    /// </summary>
    /// <author>
    ///     MDM
    /// </author>
    internal class DecoderException : Mp3SharpException
    {
        #region Constructors and Destructors

        public DecoderException(string msg, Exception t)
            : base(msg, t)
        {
            this.InitBlock();
        }

        public DecoderException(int errorcode, Exception t)
            : this(GetErrorString(errorcode), t)
        {
            this.InitBlock();
            this.ErrorCode = errorcode;
        }

        #endregion

        #region Public Properties

        public virtual int ErrorCode { get; private set; }

        #endregion

        #region Public Methods and Operators

        public static string GetErrorString(int errorcode)
        {
            // REVIEW: use resource file to map error codes
            // to locale-sensitive strings. 
            return "Decoder error code:" + Convert.ToString(errorcode, 16);
        }

        #endregion

        #region Methods

        private void InitBlock()
        {
            this.ErrorCode = DecoderErrors.UnknownError;
        }

        #endregion
    }
}