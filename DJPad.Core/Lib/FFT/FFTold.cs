namespace DJPad.Lib.FFT
{
    using System;

    public class FFTold : IFFT
    {
        private int n;

        private int nu;

        public float[] calculateMagnitude(float[] x)
        {
            // assume n is a power of 2
            this.n = x.Length;
            this.nu = (int)(Math.Log(this.n) / Math.Log(2));
            int n2 = this.n / 2;
            int nu1 = this.nu - 1;
            var xre = new float[this.n];
            var xim = new float[this.n];
            var mag = new float[n2];
            float tr, ti, p, arg, c, s;
            for (int i = 0; i < this.n; i++)
            {
                xre[i] = x[i];
                xim[i] = 0.0f;
            }
            int k = 0;

            for (int l = 1; l <= this.nu; l++)
            {
                while (k < this.n)
                {
                    for (int i = 1; i <= n2; i++)
                    {
                        p = this.bitrev(k >> nu1);
                        arg = 2 * (float)Math.PI * p / this.n;
                        c = (float)Math.Cos(arg);
                        s = (float)Math.Sin(arg);
                        tr = xre[k + n2] * c + xim[k + n2] * s;
                        ti = xim[k + n2] * c - xre[k + n2] * s;
                        xre[k + n2] = xre[k] - tr;
                        xim[k + n2] = xim[k] - ti;
                        xre[k] += tr;
                        xim[k] += ti;
                        k++;
                    }
                    k += n2;
                }
                k = 0;
                nu1--;
                n2 = n2 / 2;
            }
            k = 0;
            int r;
            while (k < this.n)
            {
                r = this.bitrev(k);
                if (r > k)
                {
                    tr = xre[k];
                    ti = xim[k];
                    xre[k] = xre[r];
                    xim[k] = xim[r];
                    xre[r] = tr;
                    xim[r] = ti;
                }
                k++;
            }

            mag[0] = (float)(Math.Sqrt(xre[0] * xre[0] + xim[0] * xim[0])) / this.n;
            for (int i = 1; i < this.n / 2; i++)
            {
                mag[i] = 2 * (float)(Math.Sqrt(xre[i] * xre[i] + xim[i] * xim[i])) / this.n;
            }
            return mag;
        }

        private int bitrev(int j)
        {
            int j2;
            int j1 = j;
            int k = 0;
            for (int i = 1; i <= this.nu; i++)
            {
                j2 = j1 / 2;
                k = 2 * k + j1 - 2 * j2;
                j1 = j2;
            }
            return k;
        }
    }
}