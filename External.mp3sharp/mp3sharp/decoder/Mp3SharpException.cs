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

    /// <summary>
    ///     The Mp3SharpException is the base class for all API-level
    ///     exceptions thrown by JavaLayer. To facilitate conversion and
    ///     common handling of exceptions from other domains, the class
    ///     can delegate some functionality to a contained Throwable instance.
    /// </summary>
    /// <author>
    ///     MDM
    /// </author>
    [Serializable]
    public class Mp3SharpException : Exception
    {
        #region Constructors and Destructors

        public Mp3SharpException()
        {
        }

        public Mp3SharpException(string msg)
            : base(msg)
        {
        }

        public Mp3SharpException(string msg, Exception t)
            : base(msg)
        {
            this.Exception = t;
        }

        #endregion

        #region Public Properties

        public virtual Exception Exception { get; private set; }

        #endregion
    }
}