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

namespace Mp3Sharp
{
    using System;

    using javazoom.jl.decoder;

    /// <summary>
    ///     Instances of <code>BitstreamException</code> are thrown when operations on a <code>Bitstream</code> fail.
    ///     The exception provides details of the exception condition in two ways:
    ///     as an error-code describing the nature of the error as the <code>Throwable</code> instance, if any, that was thrown
    ///     indicating that an exceptional condition has occurred.
    ///     @since 0.0.6
    /// </summary>
    /// <author>
    ///     MDM	12/12/99
    /// </author>
    public class BitstreamException : Mp3SharpException
    {
        #region Constructors and Destructors

        public BitstreamException(String msg, Exception t)
            : base(msg, t)
        {
            this.InitBlock();
        }

        public BitstreamException(int errorcode, Exception t)
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
            // REVIEW: use resource bundle to map error codes
            // to locale-sensitive strings.
            return "Bitstream errorcode " + Convert.ToString(errorcode);
        }

        #endregion

        #region Methods

        private void InitBlock()
        {
            this.ErrorCode = BitstreamErrorsFields.UnknownError;
        }

        #endregion
    }
}