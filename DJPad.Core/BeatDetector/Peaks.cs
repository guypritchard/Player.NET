/*
	Copyright (C) 2001, 2006 by Simon Dixon

	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License along
	with this program (the file gpl.txt); if not, download it from
	http://www.gnu.org/licenses/gpl.txt or write to the
	Free Software Foundation, Inc.,
	51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DJPad.Lib.Beatroot
{
    public class Peaks
    {

        public static bool debug = false;
        public static int pre = 3;
        public static int post = 1;

        /** General peak picking method for finding n local maxima in an array
	 *  @param data input data
	 *  @param peaks list of peak indexes
	 *  @param width minimum distance between peaks
	 */

        public static int findPeaks(double[] data, int[] peaks, int width)
        {
            int peakCount = 0;
            int maxp = 0;
            int mid = 0;
            int end = data.Length;
            while (mid < end)
            {
                int i = mid - width;
                if (i < 0)
                    i = 0;
                int stop = mid + width + 1;
                if (stop > data.Length)
                    stop = data.Length;
                maxp = i;
                for (i++; i < stop; i++)
                    if (data[i] > data[maxp])
                        maxp = i;
                if (maxp == mid)
                {
                    int j;
                    for (j = peakCount; j > 0; j--)
                    {
                        if (data[maxp] <= data[peaks[j - 1]])
                            break;
                        else if (j < peaks.Length)
                            peaks[j] = peaks[j - 1];
                    }
                    if (j != peaks.Length)
                        peaks[j] = maxp;
                    if (peakCount != peaks.Length)
                        peakCount++;
                }
                mid++;
            }
            return peakCount;
        } // findPeaks()

        /** General peak picking method for finding local maxima in an array
	 *  @param data input data
	 *  @param width minimum distance between peaks
	 *  @param threshold minimum value of peaks
	 *  @return list of peak indexes
	 */

        public static List<int> findPeaks(double[] data, int width, double threshold)
        {
            return findPeaks(data, width, threshold, 0, false);
        } // findPeaks()

        /** General peak picking method for finding local maxima in an array
	 *  @param data input data
	 *  @param width minimum distance between peaks
	 *  @param threshold minimum value of peaks
	 *  @param decayRate how quickly previous peaks are forgotten
	 *  @param isRelative minimum value of peaks is relative to local average
	 *  @return list of peak indexes
	 */

        public static List<int> findPeaks(double[] data, int width, double threshold, double decayRate, bool isRelative)
        {
            List<int> peaks = new List<int>();
            int maxp = 0;
            int mid = 0;
            int end = data.Length;
            double av = data[0];
            while (mid < end)
            {
                av = decayRate*av + (1 - decayRate)*data[mid];
                if (av < data[mid])
                    av = data[mid];
                int i = mid - width;
                if (i < 0)
                    i = 0;
                int stop = mid + width + 1;
                if (stop > data.Length)
                    stop = data.Length;
                maxp = i;
                for (i++; i < stop; i++)
                    if (data[i] > data[maxp])
                        maxp = i;
                if (maxp == mid)
                {
                    if (overThreshold(data, maxp, width, threshold, isRelative, av))
                    {
                        if (debug)
                        {
                            Debug.WriteLine(" peak");
                        }

                        peaks.Add(maxp);
                    }
                    else if (debug)
                        Debug.WriteLine("");
                }
                mid++;
            }
            return peaks;
        } // findPeaks()

        public static double expDecayWithHold(double av, double decayRate,
            double[] data, int start, int stop)
        {
            while (start < stop)
            {
                av = decayRate*av + (1 - decayRate)*data[start];
                if (av < data[start])
                    av = data[start];
                start++;
            }
            return av;
        } // expDecayWithHold()

        public static bool overThreshold(double[] data, int index, int width, double threshold, bool isRelative,
            double av)
        {
            if (debug)
                Debug.WriteLine("%4d : %6.3f     Av1: %6.3f    ", index, data[index], av);
            if (data[index] < av)
                return false;
            if (isRelative)
            {
                int iStart = index - pre*width;
                if (iStart < 0)
                    iStart = 0;
                int iStop = index + post*width;
                if (iStop > data.Length)
                    iStop = data.Length;
                double sum = 0;
                int count = iStop - iStart;
                while (iStart < iStop)
                    sum += data[iStart++];
                if (debug)
                    Debug.WriteLine("    %6.3f    %6.3f   ", sum/count, data[index] - sum/count - threshold);
                return (data[index] > sum/count + threshold);
            }

            return (data[index] > threshold);
        } // overThreshold()

        public static void normalise(double[] data)
        {
            double sx = 0;
            double sxx = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sx += data[i];
                sxx += data[i]*data[i];
            }
            double mean = sx/data.Length;
            double sd = Math.Sqrt((sxx - sx*mean)/data.Length);
            if (sd == 0)
                sd = 1; // all data[i] == mean  -> 0; avoids div by 0
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (data[i] - mean)/sd;
            }
        } // normalise()

        /** Uses an n-point linear regression to estimate the slope of data.
	 *  @param data input data
	 *  @param hop spacing of data points
	 *  @param n length of linear regression
	 *  @param slope output data
	 */

        public static void getSlope(double[] data, double hop, int n,
            double[] slope)
        {
            int i = 0, j = 0;
            double t;
            double sx = 0, sxx = 0, sy = 0, sxy = 0;
            for (; i < n; i++)
            {
                t = i*hop;
                sx += t;
                sxx += t*t;
                sy += data[i];
                sxy += t*data[i];
            }
            double delta = n*sxx - sx*sx;
            for (; j < n/2; j++)
                slope[j] = (n*sxy - sx*sy)/delta;
            for (; j < data.Length - (n + 1)/2; j++, i++)
            {
                slope[j] = (n*sxy - sx*sy)/delta;
                sy += data[i] - data[i - n];
                sxy += hop*(n*data[i] - sy);
            }
            for (; j < data.Length; j++)
                slope[j] = (n*sxy - sx*sy)/delta;
        } // getSlope()

        public static double min(double[] arr)
        {
            return arr[imin(arr)];
        }

        public static double max(double[] arr)
        {
            return arr[imax(arr)];
        }

        public static int imin(double[] arr)
        {
            int i = 0;
            for (int j = 1; j < arr.Length; j++)
                if (arr[j] < arr[i])
                    i = j;
            return i;
        } // imin()

        public static int imax(double[] arr)
        {
            int i = 0;
            for (int j = 1; j < arr.Length; j++)
                if (arr[j] > arr[i])
                    i = j;
            return i;
        } // imax()

    } // class Peaks
}
