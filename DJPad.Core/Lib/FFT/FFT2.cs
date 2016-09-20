namespace DJPad.Lib.FFT
{
    using System;

    public class KJFFT : IFFT
    {
        private readonly float[] mag;

        private readonly int nu;

        private readonly int sampleSize;

        private readonly int ss2;

        private readonly float[] xim;

        private readonly float[] xre;

        private int[] fftBr;

        private float[] fftCos;

        private float[] fftSin;

        /**
         * @param The amount of the sample provided to the "calculate" method to use
         *            during FFT calculations.
         */

        public KJFFT(int sampleSize)
        {
            this.sampleSize = sampleSize;
            this.ss2 = sampleSize >> 1;

            this.xre = new float[sampleSize];
            this.xim = new float[sampleSize];
            this.mag = new float[this.ss2];

            this.nu = (int)(Math.Log(sampleSize) / Math.Log(2));

            this.prepareFFTTables();
        }

        /**
         * @param pSample
         *            The sample to compute FFT values on.
         * @return The results of the calculation, normalized between 0.0 and 1.0.
         */

        public float[] calculateMagnitude(float[] pSample)
        {
            int n2 = this.ss2;
            int nu1 = this.nu - 1;

            int wAps = pSample.Length / this.sampleSize;

            // -- FIXME: This affects the calculation accuracy, because
            // is compresses the digital signal. Looks nice on
            // the spectrum analyser, as it chops off most of
            // sound we cannot hear anyway.
            for (int a = 0, b = 0; a < pSample.Length && b < this.sampleSize; a += wAps, b++)
            {
                this.xre[b] = pSample[a];
                this.xim[b] = 0.0f;
            }

            float tr, ti, c, s;
            int k, kn2, x = 0;

            for (int l = 1; l <= this.nu; l++)
            {
                k = 0;

                while (k < this.sampleSize)
                {
                    for (int i = 1; i <= n2; i++)
                    {
                        // -- Tabled sin/cos
                        c = this.fftCos[x];
                        s = this.fftSin[x];

                        kn2 = k + n2;

                        tr = this.xre[kn2] * c + this.xim[kn2] * s;
                        ti = this.xim[kn2] * c - this.xre[kn2] * s;

                        this.xre[kn2] = this.xre[k] - tr;
                        this.xim[kn2] = this.xim[k] - ti;
                        this.xre[k] += tr;
                        this.xim[k] += ti;

                        k++;
                        x++;
                    }

                    k += n2;
                }

                nu1--;
                n2 >>= 1;
            }

            int r;

            // -- Reorder output.
            for (k = 0; k < this.sampleSize; k++)
            {
                // -- Use tabled BR values.
                r = this.fftBr[k];

                if (r > k)
                {
                    tr = this.xre[k];
                    ti = this.xim[k];

                    this.xre[k] = this.xre[r];
                    this.xim[k] = this.xim[r];
                    this.xre[r] = tr;
                    this.xim[r] = ti;
                }
            }

            // -- Calculate magnitude.
            this.mag[0] = (float)(Math.Sqrt(this.xre[0] * this.xre[0] + this.xim[0] * this.xim[0])) / this.sampleSize;

            for (int i = 1; i < this.ss2; i++)
            {
                this.mag[i] = 2 * (float)(Math.Sqrt(this.xre[i] * this.xre[i] + this.xim[i] * this.xim[i]))
                              / this.sampleSize;
            }

            return this.mag;
        }

        private int bitrev(int j, int nu)
        {
            int j1 = j;
            int j2;
            int k = 0;

            for (int i = 1; i <= nu; i++)
            {
                j2 = j1 >> 1;
                k = (k << 1) + j1 - (j2 << 1);
                j1 = j2;
            }

            return k;
        }

        private void prepareFFTTables()
        {
            int n2 = this.ss2;
            int nu1 = this.nu - 1;

            // -- Allocate FFT SIN/COS tables.
            this.fftSin = new float[this.nu * n2];
            this.fftCos = new float[this.nu * n2];

            float p, arg;
            int k = 0, x = 0;

            // -- Prepare SIN/COS tables.
            for (int l = 1; l <= this.nu; l++)
            {
                while (k < this.sampleSize)
                {
                    for (int i = 1; i <= n2; i++)
                    {
                        p = this.bitrev(k >> nu1, this.nu);

                        arg = 2 * (float)Math.PI * p / this.sampleSize;

                        this.fftSin[x] = (float)Math.Sin(arg);
                        this.fftCos[x] = (float)Math.Cos(arg);

                        k++;
                        x++;
                    }

                    k += n2;
                }

                k = 0;

                nu1--;
                n2 >>= 1;
            }

            // -- Prepare bitrev table.
            this.fftBr = new int[this.sampleSize];

            for (k = 0; k < this.sampleSize; k++)
            {
                this.fftBr[k] = this.bitrev(k, this.nu);
            }
        }
    }
}