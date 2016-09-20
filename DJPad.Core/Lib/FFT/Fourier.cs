namespace DJPad.Lib.FFT
{
    using System;

    public class Fourier
    {
        public const int FFTLEN = 2048;

        public static void fft_double(
            uint NumberOfSamples,
            bool InverseTransform,
            double[] RealIn,
            double[] ImagIn,
            ref double[] RealOut,
            ref double[] ImagOut)
        {
            uint NumBits;
            uint i, j, k, n;
            uint BlockSize, BlockEnd;

            double angle_numerator = 2.0 * Math.PI;
            double tr, ti;

            if (!IsPowerOfTwo(NumberOfSamples))
            {
                return;
            }

            if (InverseTransform)
            {
                angle_numerator = -angle_numerator;
            }

            NumBits = NumberOfBitsNeeded(NumberOfSamples);

            for (i = 0; i < NumberOfSamples; i++)
            {
                j = ReverseBits(i, NumBits);
                RealOut[j] = RealIn[i];
                ImagOut[j] = (ImagIn == null) ? 0.0 : ImagIn[i];
            }

            BlockEnd = 1;
            for (BlockSize = 2; BlockSize <= NumberOfSamples; BlockSize <<= 1)
            {
                double delta_angle = angle_numerator / BlockSize;
                double sm2 = Math.Sin(-2 * delta_angle);
                double sm1 = Math.Sin(-delta_angle);
                double cm2 = Math.Cos(-2 * delta_angle);
                double cm1 = Math.Cos(-delta_angle);
                double w = 2 * cm1;
                var ar = new double[3];
                var ai = new double[3];

                for (i = 0; i < NumberOfSamples; i += BlockSize)
                {
                    ar[2] = cm2;
                    ar[1] = cm1;

                    ai[2] = sm2;
                    ai[1] = sm1;

                    for (j = i, n = 0; n < BlockEnd; j++, n++)
                    {
                        ar[0] = w * ar[1] - ar[2];
                        ar[2] = ar[1];
                        ar[1] = ar[0];

                        ai[0] = w * ai[1] - ai[2];
                        ai[2] = ai[1];
                        ai[1] = ai[0];

                        k = j + BlockEnd;
                        tr = ar[0] * RealOut[k] - ai[0] * ImagOut[k];
                        ti = ar[0] * ImagOut[k] + ai[0] * RealOut[k];

                        RealOut[k] = RealOut[j] - tr;
                        ImagOut[k] = ImagOut[j] - ti;

                        RealOut[j] += tr;
                        ImagOut[j] += ti;
                    }
                }

                BlockEnd = BlockSize;
            }

            if (InverseTransform)
            {
                double denom = NumberOfSamples;

                for (i = 0; i < NumberOfSamples; i++)
                {
                    RealOut[i] /= denom;
                    ImagOut[i] /= denom;
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // check is a number is a power of 2
        //////////////////////////////////////////////////////////////////////////////////////
        private static bool IsPowerOfTwo(uint X)
        {
            if (X % 2 != 0)
            {
                return false;
            }

            if ((X & (X - 1)) != 0)
            {
                return false;
            }

            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // return needed bits for fft
        //////////////////////////////////////////////////////////////////////////////////////
        private static uint NumberOfBitsNeeded(uint NumberOfSamples)
        {
            if ((NumberOfSamples % 2) != 0)
            {
                return 0;
            }

            uint i = 0;

            while ((NumberOfSamples & (1 << (int)i)) == 0)
            {
                i++;
            }

            return i;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // ?
        //////////////////////////////////////////////////////////////////////////////////////

        private static uint ReverseBits(uint Index, uint Bits)
        {
            uint i, rev;

            for (i = rev = 0; i < Bits; i++)
            {
                rev = (rev << 1) | (Index & 1);
                Index >>= 1;
            }

            return rev;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // return a frequency from the basefreq and num of samples
        //////////////////////////////////////////////////////////////////////////////////////
        private static double Index_to_frequency(uint BaseFreq, uint NumberOfSamples, uint Index)
        {
            if (Index >= NumberOfSamples)
            {
                return 0.0;
            }
            
            if (Index <= NumberOfSamples / 2)
            {
                return (Index / (double)NumberOfSamples * BaseFreq);
            }
            
            return (-(double)(NumberOfSamples - Index) / NumberOfSamples * BaseFreq);
        }
    }
}