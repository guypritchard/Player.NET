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
    using System;

    /// <summary>
    ///     The <code>Equalizer</code> class can be used to specify
    ///     equalization settings for the MPEG audio decoder.
    ///     <p>
    ///         The equalizer consists of 32 band-pass filters.
    ///         Each band of the equalizer can take on a fractional value between
    ///         -1.0 and +1.0.
    ///         At -1.0, the input signal is attenuated by 6dB, at +1.0 the signal is
    ///         amplified by 6dB.
    /// </summary>
    /// <seealso cref="">
    ///     Decoder
    /// </seealso>
    /// <author>
    ///     MDM
    /// </author>
    internal class Equalizer
    {
        #region Constants

        private const int Bands = 32;

        #endregion

        #region Static Fields

        /// <summary>
        ///     Equalizer setting to denote that a given band will not be
        ///     present in the output signal.
        /// </summary>
        public static readonly float BandNotPresent = Single.NegativeInfinity;

        public static readonly Equalizer PassThruEq = new Equalizer();

        #endregion

        #region Fields

        private float[] settings;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Creates a new <code>Equalizer</code> instance.
        /// </summary>
        public Equalizer()
        {
            this.InitBlock();
        }

        public Equalizer(float[] settings)
        {
            this.InitBlock();
            this.FromFloatArray = settings;
        }

        public Equalizer(EqFunction eq)
        {
            this.InitBlock();
            this.FromEQFunction = eq;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Retrieves the number of bands present in this equalizer.
        /// </summary>
        public virtual int BandCount
        {
            get
            {
                return this.settings.Length;
            }
        }

        public virtual EqFunction FromEQFunction
        {
            set
            {
                this.Reset();
                int max = Bands;

                for (int i = 0; i < max; i++)
                {
                    this.settings[i] = Equalizer.Limit(value.GetBand(i));
                }
            }
        }

        /// <summary>
        ///     Sets the bands of this equalizer to the value the bands of another equalizer. Bands that are not present in both equalizers are ignored.
        /// </summary>
        public virtual Equalizer FromEqualizer
        {
            set
            {
                if (value != this)
                {
                    this.FromFloatArray = value.settings;
                }
            }
        }

        public virtual float[] FromFloatArray
        {
            set
            {
                this.Reset();
                int max = (value.Length > Bands) ? Bands : value.Length;

                for (int i = 0; i < max; i++)
                {
                    this.settings[i] = Equalizer.Limit(value[i]);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Retrieves an array of floats whose values represent a
        ///     scaling factor that can be applied to linear samples
        ///     in each band to provide the equalization represented by
        ///     this instance.
        /// </summary>
        /// <returns>
        ///     An array of factors that can be applied to the
        ///     sub-bands.
        /// </returns>
        internal virtual float[] BandFactors
        {
            get
            {
                var factors = new float[Bands];
                for (int i = 0, maxCount = Bands; i < maxCount; i++)
                {
                    factors[i] = Equalizer.GetBandFactor(this.settings[i]);
                }

                return factors;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Retrieves the eq setting for a given band.
        /// </summary>
        public float GetBand(int band)
        {
            float eq = 0.0f;

            if ((band >= 0) && (band < Bands))
            {
                eq = this.settings[band];
            }

            return eq;
        }

        /// <summary>
        ///     Sets all bands to 0.0
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < Bands; i++)
            {
                this.settings[i] = 0.0f;
            }
        }

        public float SetBand(int band, float neweq)
        {
            float eq = 0.0f;

            if ((band >= 0) && (band < Bands))
            {
                eq = this.settings[band];
                this.settings[band] = Equalizer.Limit(neweq);
            }

            return eq;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Converts an equalizer band setting to a sample factor.
        ///     The factor is determined by the function f = 2^n where
        ///     n is the equalizer band setting in the range [-1.0,1.0].
        /// </summary>
        public static float GetBandFactor(float eq)
        {
            if (eq == BandNotPresent)
            {
                return 0.0f;
            }

            var f = (float)Math.Pow(2.0, eq);
            return f;
        }

        private void InitBlock()
        {
            this.settings = new float[Bands];
        }

        private static float Limit(float eq)
        {
            if (eq == BandNotPresent)
            {
                return eq;
            }

            if (eq > 1.0f)
            {
                return 1.0f;
            }

            if (eq < - 1.0f)
            {
                return - 1.0f;
            }

            return eq;
        }

        #endregion

        internal abstract class EqFunction
        {
            #region Public Methods and Operators

            /// <summary>
            ///     Returns the setting of a band in the equalizer.
            /// </summary>
            /// <param name="band">
            ///     index of the band to retrieve the setting for.
            /// </param>
            /// <returns>
            ///     The setting of the specified band. This is a value between -1 and +1.
            /// </returns>
            public virtual float GetBand(int band)
            {
                return 0.0f;
            }

            #endregion
        }
    }
}