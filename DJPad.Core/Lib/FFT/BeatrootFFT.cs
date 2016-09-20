namespace DJPad.Lib.FFT
{
    using System.Linq;
    using DJPad.Lib.Beatroot;
    using DJPad.Lib.FFT;

    public class BeatrootFFT :IFFT
    {
        public float[] calculateMagnitude(float[] x)
        {
            FFT.powerFFT(x.Select(i => (double)i).Take(4096).ToArray());
            return x;
        }
    }
}
