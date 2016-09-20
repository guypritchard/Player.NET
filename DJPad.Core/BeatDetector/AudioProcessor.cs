/*  BeatRoot: An interactive beat tracking system
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
	http://www.gnu.org/licenses/gpl.txt or write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

namespace DJPad.Lib.Beatroot
{
    /** Audio processing class (adapted from PerformanceMatcher). */
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using DJPad.Core.Interfaces;
    using DJPad.Core;
    
    public class AudioProcessor
    {
        /** Uncompressed version of <code>rawInputStream</code>.
         *  In the (normal) case where the input is already PCM data,
         *  <code>rawInputStream == pcmInputStream</code> */

        /** Standard input for interactive prompts (for debugging). */
        // BufferedReader stdIn;

        /** Object for plotting output (for debugging / development) . */
        // Plot plot;

        /** Flag for enabling or disabling debugging output */
        public static bool debug = false;

        /** Flag for plotting onset detection function. */
        public static bool doOnsetPlot = false;

        /** Flag for suppressing all standard output messages except results. */
        protected static bool silent = true;

        /** Flag for batch mode. */
        public static bool batchMode = false;

        /** RMS frame energy below this value results in the frame being set to zero,
         *  so that normalisation does not have undesired side-effects. */
        public static double silenceThreshold = 0.0004;

        /** For dynamic range compression, this value is added to the log magnitude
         *  in each frequency bin and any remaining negative values are then set to zero.
         */
        public static double rangeThreshold = 10;

        /** Determines method of normalisation. Values can be:<ul>
         *  <li>0: no normalisation</li>
         *  <li>1: normalisation by current frame energy</li>
         *  <li>2: normalisation by exponential average of frame energy</li>
         *  </ul>
         */
        public static int normaliseMode = 2;

        /** Ratio between rate of sampling the signal energy (for the amplitude envelope) and the hop size */
        public static int energyOversampleFactor = 2;

        /** Audio buffer for live input. (Not used yet) */
        public static int liveInputBufferSize = 32768; /* ~195ms buffer @CD */

        /** Maximum file length in seconds. Used for static allocation of arrays. */
        public static int MAX_LENGTH = 3600; // i.e. 1 hour
        protected string audioFileName;
        protected FormatInformation audioFormat;


        private int buffSize;
        protected int cbIndex;
        protected int channels;
        protected double[] circBuffer;
        protected double[] energy;
        protected int fftSize;
        protected double fftTime;
        protected int frameCount;

        /** RMS amplitude of the current frame. */
        protected double frameRMS;
        protected double[,] frames;
        protected int[] freqMap;

        /** The number of entries in <code>freqMap</code>. Note that the length of
         *  the array is greater, because its size is not known at creation time. */
        protected int freqMapSize;
        protected int hopSize;
        protected double hopTime;
        protected double[] imBuffer;
        protected byte[] inputBuffer;
        protected double ltAverage;
        protected double[] newFrame;
        protected List<Event> onsetList;
        protected double[] onsets;
        protected ISampleSource pcmInputStream;
        protected double[] phaseDeviation;
        protected double[] prevFrame;
        protected double[] prevPhase;

        /** Phase of the frame before the previous frame, for calculating an
         *  onset function based on spectral phase deviation. */
        protected double[] prevPrevPhase;
        protected double[] reBuffer;
        protected float sampleRate;
        protected double[] spectralFlux;
        protected int totalFrames;
        protected double[] window;
        protected double[] y2Onsets;

        /** Constructor: note that streams are not opened until the input file is set
         *  (see <code>setInputFile()</code>). */

        public AudioProcessor()
        {
            cbIndex = 0;
            frameRMS = 0;
            ltAverage = 0;
            frameCount = 0;
            hopSize = 0;
            fftSize = 0;
            hopTime = 0.010; // DEFAULT, overridden with -h
            fftTime = 0.04644; // DEFAULT, overridden with -f
            // progressCallback = null;
            //if (doOnsetPlot)
            //    plot = new Plot();
        } // constructor

        /** For debugging, outputs information about the AudioProcessor to
         *  standard error.
         */

        public void print()
        {
            // System.err.println(this);
        } // print()

        /** For interactive pause - wait for user to hit Enter */
        //public string readLine()
        //{
        //    try { return stdIn.readLine(); }
        //    catch (Exception e) { return null; }
        //} // readLine()

        /** Gives some basic information about the audio being processed. */

        public override string ToString()
        {
            return "AudioProcessor\n" +
                   string.Format("\tFile: %s (%3.1f kHz, %1d channels)\n",
                       audioFileName, sampleRate/1000, channels) +
                   string.Format("\tHop / FFT sizes: %5.3f / %5.3f",
                       hopTime, hopTime*fftSize/hopSize);
        } // toString()

        /** Adds a link to the GUI component which shows the progress of matching.
         *  @param c the AudioProcessor representing the other performance 
         */
        //public void setProgressCallback(ProgressIndicator c)
        //{
        //    progressCallback = c;
        //} // setProgressCallback()

        /** Sets up the streams and buffers for live audio input (CD quality).
         *  If any Exception is thrown within this method, it is caught, and any
         *  opened streams are closed, and <code>pcmInputStream</code> is set to
         *  <code>null</code>, indicating that the method did not complete
         *  successfully.
         */
        //public void setLiveInput()
        //{
        //    try
        //    {
        //        channels = 2;
        //        sampleRate = 44100;
        //        AudioFormat desiredFormat = new AudioFormat(
        //                    AudioFormat.Encoding.PCM_SIGNED, sampleRate, 16,
        //                    channels, channels * 2, sampleRate, false);
        //        TargetDataLine tdl = AudioSystem.getTargetDataLine(desiredFormat);
        //        tdl.open(desiredFormat, liveInputBufferSize);
        //        pcmInputStream = new AudioInputStream(tdl);
        //        audioFormat = pcmInputStream.getFormat();
        //        init();
        //        tdl.start();
        //    }
        //    catch (Exception e)
        //    {
        //        e.printStackTrace();
        //        closeStreams();	// make sure it exits in a consistent state
        //    }
        //} // setLiveInput()

        /** Sets up the streams and buffers for audio file input.
         *  If any Exception is thrown within this method, it is caught, and any
         *  opened streams are closed, and <code>pcmInputStream</code> is set to
         *  <code>null</code>, indicating that the method did not complete
         *  successfully.
         *  @param fileName The path name of the input audio file.
         */

        public void setInputFile(ISampleSource source)
        {
            try
            {
                audioFormat = source.GetFormat();
                channels = audioFormat.Channels;
                sampleRate = audioFormat.SampleRate;
                pcmInputStream = source;
                init();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        //public void setInputFile(String fileName) {
        //    closeStreams();		// release previously allocated resources
        //    audioFileName = fileName;
        //    try {
        //        if (audioFileName == null)
        //        {
        //            throw new Exception("No input file specified");
        //        }

        //        File audioFile = new File(audioFileName);
        //        if (!audioFile.isFile())
        //        {				
        //            throw new FileNotFoundException("Requested file does not exist: " + audioFileName);
        //        }

        //        rawInputStream = AudioSystem.getAudioInputStream(audioFile);
        //        audioFormat = rawInputStream.getFormat();
        //        channels = audioFormat.getChannels();
        //        sampleRate = audioFormat.getSampleRate();
        //        pcmInputStream = rawInputStream;
        //        if ((audioFormat.getEncoding()!=AudioFormat.Encoding.PCM_SIGNED) ||
        //                (audioFormat.getFrameSize() != channels * 2) ||
        //                audioFormat.isBigEndian()) {
        //            AudioFormat desiredFormat = new AudioFormat(
        //                    AudioFormat.Encoding.PCM_SIGNED, sampleRate, 16,
        //                    channels, channels * 2, sampleRate, false);
        //            pcmInputStream = AudioSystem.getAudioInputStream(desiredFormat, rawInputStream);
        //            audioFormat = desiredFormat;
        //        }
        //        init();
        //    } catch (Exception e) {
        //        e.printStackTrace();
        //        closeStreams();	// make sure it exits in a consistent state
        //    }
        //} // setInputFile()


        /** Allocates memory for arrays, based on parameter settings */

        protected void init()
        {
            hopSize = (int) Math.Round(sampleRate*hopTime);
            fftSize = (int) Math.Round(Math.Pow(2, Math.Round(Math.Log(fftTime*sampleRate)/Math.Log(2))));
            makeFreqMap(fftSize, sampleRate);
            buffSize = hopSize*channels*2;
            if ((inputBuffer == null) || (inputBuffer.Length != buffSize))
                inputBuffer = new byte[buffSize];
            if ((circBuffer == null) || (circBuffer.Length != fftSize))
            {
                circBuffer = new double[fftSize];
                reBuffer = new double[fftSize];
                imBuffer = new double[fftSize];
                prevPhase = new double[fftSize];
                prevPrevPhase = new double[fftSize];
                prevFrame = new double[fftSize];
                window = FFT.makeWindow(FFT.HAMMING, fftSize, fftSize);
                for (int i = 0; i < fftSize; i++)
                    window[i] *= Math.Sqrt(fftSize);
            }
        
                //if (pcmInputStream == rawInputStream)
                //{
                //    totalFrames = (int) (pcmInputStream.getFrameLength()/hopSize);
                //}
            
            //else
            //{
            //    totalFrames = (int) (MAX_LENGTH/hopTime);
            //}

            totalFrames = 18432;

            if ((newFrame == null) || (newFrame.Length != freqMapSize))
            {
                newFrame = new double[freqMapSize];
                frames = new double[totalFrames, freqMapSize];
            }
            else if (frames.Length != totalFrames)
            {
                frames = new double[totalFrames, freqMapSize];
            }

            energy = new double[totalFrames*energyOversampleFactor];
            phaseDeviation = new double[totalFrames];
            spectralFlux = new double[totalFrames];
            frameCount = 0;
            cbIndex = 0;
            frameRMS = 0;
            ltAverage = 0;
            // progressCallback = null;
        } // init()

        /** Closes the input stream(s) associated with this object. */
        //public void closeStreams()
        //{
        //    if (pcmInputStream != null)
        //    {
        //        try
        //        {
        //            pcmInputStream.close();
        //            if (pcmInputStream != rawInputStream)
        //                rawInputStream.close();
        //            if (audioOut != null)
        //            {
        //                audioOut.drain();
        //                audioOut.close();
        //            }
        //        }
        //        catch (Exception e) { }
        //        pcmInputStream = null;
        //        audioOut = null;
        //    }
        //} // closeStreams()

        /** Creates a map of FFT frequency bins to comparison bins.
         *  Where the spacing of FFT bins is less than 0.5 semitones, the mapping is
         *  one to one. Where the spacing is greater than 0.5 semitones, the FFT
         *  energy is mapped into semitone-wide bins. No scaling is performed; that
         *  is the energy is summed into the comparison bins. See also
         *  processFrame()
         */

        protected void makeFreqMap(int fftSize, float sampleRate)
        {
            freqMap = new int[fftSize/2 + 1];
            double binWidth = sampleRate/fftSize;
            var crossoverBin = (int) (2/(Math.Pow(2, 1/12.0) - 1));
            var crossoverMidi = (int) Math.Round(Math.Log(crossoverBin*binWidth/440)/Math.Log(2)*12 + 69);

            // freq = 440 * Math.pow(2, (midi-69)/12.0) / binWidth;
            int i = 0;
            
            while (i <= crossoverBin)
            {
                freqMap[i++] = i;
            }

            while (i <= fftSize/2)
            {
                double midi = Math.Log(i*binWidth/440)/Math.Log(2)*12 + 69;
                if (midi > 127)
                    midi = 127;
                freqMap[i++] = crossoverBin + (int) Math.Round(midi) - crossoverMidi;
            }
            freqMapSize = freqMap[i - 1] + 1;
        } // makeFreqMap()

        /** Calculates the weighted phase deviation onset detection function.
         *  Not used.
         *  TODO: Test the change to WPD fn */

        protected void weightedPhaseDeviation()
        {
            if (frameCount < 2)
                phaseDeviation[frameCount] = 0;
            else
            {
                for (int i = 0; i < fftSize; i++)
                {
                    double pd = imBuffer[i] - 2*prevPhase[i] + prevPrevPhase[i];
                    double pd1 = Math.Abs(Math.IEEERemainder(pd, 2*Math.PI));
                    phaseDeviation[frameCount] += pd1*reBuffer[i];
                    // System.err.printf("%7.3f   %7.3f\n", pd/Math.PI, pd1/Math.PI);
                }
            }
            phaseDeviation[frameCount] /= fftSize*Math.PI;
            double[] tmp = prevPrevPhase;
            prevPrevPhase = prevPhase;
            prevPhase = imBuffer;
            imBuffer = tmp;
        } // weightedPhaseDeviation()

        /** Reads a frame of input data, averages the channels to mono, scales
         *  to a maximum possible absolute value of 1, and stores the audio data
         *  in a circular input buffer.
         *  @return true if a frame (or part of a frame, if it is the final frame)
         *  is read. If a complete frame cannot be read, the InputStream is set
         *  to null.
         */

        public bool getFrame()
        {
            try
            {
                Sample sample = pcmInputStream.GetSample(buffSize);
                //if ((audioOut != null) && (bytesRead > 0))
                //    if (audioOut.write(inputBuffer, 0, bytesRead) != bytesRead)
                //        Trace.WriteLine("Error writing to audio device");
                if (sample.DataLength < buffSize)
                {
                    if (!silent)
                        Debug.WriteLine("End of input: " + audioFileName);
                    return false;
                }

                Array.Copy(sample.Data, inputBuffer, buffSize);
            }
            catch (IOException e)
            {
                Trace.WriteLine(e);
                return false;
            }

            frameRMS = 0;
            double sampleValue;
            switch (channels)
            {
                case 1:
                    for (int i = 0; i < inputBuffer.Length; i += 2)
                    {
                        sampleValue = ((inputBuffer[i + 1] << 8) | (inputBuffer[i] & 0xff))/32768.0;
                        frameRMS += sampleValue*sampleValue;
                        circBuffer[cbIndex++] = sampleValue;
                        if (cbIndex == fftSize)
                        {
                            cbIndex = 0;
                        }
                    }
                    break;
                case 2: // saves ~0.1% of RT (total input overhead ~0.4%) :)
                    for (int i = 0; i < inputBuffer.Length; i += 4)
                    {
                        sampleValue = (((inputBuffer[i + 1] << 8) | (inputBuffer[i] & 0xff)) +
                                       ((inputBuffer[i + 3] << 8) | (inputBuffer[i + 2] & 0xff)))
                                      /65536.0;
                        frameRMS += sampleValue*sampleValue;
                        circBuffer[cbIndex++] = sampleValue;
                        if (cbIndex == fftSize)
                        {
                            cbIndex = 0;
                        }
                    }
                    break;
                default:
                    for (int i = 0; i < inputBuffer.Length;)
                    {
                        sampleValue = 0;
                        for (int j = 0; j < channels; j++, i += 2)
                        {
                            sampleValue += (inputBuffer[i + 1] << 8) | (inputBuffer[i] & 0xff);
                        }

                        sampleValue /= 32768.0*channels;
                        frameRMS += sampleValue*sampleValue;
                        circBuffer[cbIndex++] = sampleValue;
                        if (cbIndex == fftSize)
                            cbIndex = 0;
                    }
                    break;
            }
            frameRMS = Math.Sqrt(frameRMS/inputBuffer.Length*2*channels);
            return true;
        } // getFrame()

        public static void Fill<T>(T[] array, int start, int count, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (start + count > array.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            for (int i = start; i < start + count; i++)
            {
                array[i] = value;
            }
        }

        /** Processes a frame of audio data by first computing the STFT with a
         *  Hamming window, then mapping the frequency bins into a part-linear
         *  part-logarithmic array, then computing the spectral flux 
         *  then (optionally) normalising and calculating onsets.
         */

        public bool processFrame()
        {
            if (getFrame())
            {
                for (int i = 0; i < fftSize; i++)
                {
                    reBuffer[i] = window[i]*circBuffer[cbIndex];
                    if (++cbIndex == fftSize)
                        cbIndex = 0;
                }

                Fill(imBuffer, 0, imBuffer.Length, 0);
                FFT.magnitudePhaseFFT(reBuffer, imBuffer);
                Array.Clear(newFrame, 0, newFrame.Length);
                double flux = 0;
                for (int i = 0; i <= fftSize/2; i++)
                {
                    if (reBuffer[i] > prevFrame[i])
                    {
                        flux += reBuffer[i] - prevFrame[i];
                    }

                    newFrame[freqMap[i]] += reBuffer[i];
                }

                spectralFlux[frameCount] = flux;

                for (int i = 0; i < freqMapSize; i++)
                {
                    frames[frameCount, i] = newFrame[i];
                }

                int index = cbIndex - (fftSize - hopSize);
                if (index < 0)
                    index += fftSize;
                int sz = (fftSize - hopSize)/energyOversampleFactor;
                for (int j = 0; j < energyOversampleFactor; j++)
                {
                    double newEnergy = 0;
                    for (int i = 0; i < sz; i++)
                    {
                        newEnergy += circBuffer[index]*circBuffer[index];
                        if (++index == fftSize)
                            index = 0;
                    }
                    energy[frameCount*energyOversampleFactor + j] =
                        newEnergy/sz <= 1e-6 ? 0 : Math.Log(newEnergy/sz) + 13.816;
                }
                double decay = frameCount >= 200
                    ? 0.99
                    : (frameCount < 100 ? 0 : (frameCount - 100)/100.0);
                if (ltAverage == 0)
                    ltAverage = frameRMS;
                else
                    ltAverage = ltAverage*decay + frameRMS*(1.0 - decay);
                if (frameRMS <= silenceThreshold)
                    for (int i = 0; i < freqMapSize; i++)
                        frames[frameCount, i] = 0;
                else
                {
                    if (normaliseMode == 1)
                        for (int i = 0; i < freqMapSize; i++)
                            frames[frameCount, i] /= frameRMS;
                    else if (normaliseMode == 2)
                        for (int i = 0; i < freqMapSize; i++)
                            frames[frameCount, i] /= ltAverage;
                    for (int i = 0; i < freqMapSize; i++)
                    {
                        frames[frameCount, i] = Math.Log(frames[frameCount, i]) + rangeThreshold;
                        if (frames[frameCount, i] < 0)
                            frames[frameCount, i] = 0;
                    }
                }
                //			weightedPhaseDeviation();
                //			if (debug)
                //				System.err.printf("PhaseDev:  t=%7.3f  phDev=%7.3f  RMS=%7.3f\n",
                //						frameCount * hopTime,
                //						phaseDeviation[frameCount],
                //						frameRMS);
                double[] tmp = prevFrame;
                prevFrame = reBuffer;
                reBuffer = tmp;
                frameCount++;
                if ((frameCount%100) == 0)
                {
                    if (!silent)
                    {
                        Debug.WriteLine("Progress: {0} {1} {2}\n", frameCount, frameRMS, ltAverage);
                        // Profile.report();
                    }
                    //if ((progressCallback != null) && (totalFrames > 0))
                    //    progressCallback.setFraction((double)frameCount / totalFrames);
                }


                

                return true;
            }

                double p1 = 0.35;
                double p2 = 0.84;

            Peaks.normalise(spectralFlux);
            findOnsets(p1, p2);
            return false;
        } // processFrame()

        /** Processes a complete file of audio data. */
        //public void processFile()
        //{
        //    while (pcmInputStream != null)
        //    {
        //        // Profile.start(0);
        //        processFrame();
        //        // Profile.log(0);
        //        if (Thread.currentThread().isInterrupted())
        //        {
        //            System.err.println("info: INTERRUPTED in processFile()");
        //            return;
        //        }
        //    }

        //    //		double[] x1 = new double[phaseDeviation.length];
        //    //		for (int i = 0; i < x1.length; i++) {
        //    //			x1[i] = i * hopTime;
        //    //			phaseDeviation[i] = (phaseDeviation[i] - 0.4) * 100;
        //    //		}
        //    //		double[] x2 = new double[energy.length];
        //    //		for (int i = 0; i < x2.length; i++)
        //    //			x2[i] = i * hopTime / energyOversampleFactor;
        //    //		// plot.clear();
        //    //		plot.addPlot(x1, phaseDeviation, Color.green, 7);
        //    //		plot.addPlot(x2, energy, Color.red, 7);
        //    //		plot.setTitle("Test phase deviation");
        //    //		plot.fitAxes();

        //    //		double[] slope = new double[energy.length];
        //    //		double hop = hopTime / energyOversampleFactor;
        //    //		Peaks.getSlope(energy, hop, 15, slope);
        //    //		LinkedList<Integer> peaks = Peaks.findPeaks(slope, (int)Math.round(0.06 / hop), 10);

        //    //		double hop = hopTime;
        //    //		Peaks.normalise(spectralFlux);
        //    //		LinkedList<Integer> peaks = Peaks.findPeaks(spectralFlux, (int)Math.round(0.06 / hop), 0.35, 0.84, true);
        //    //		onsets = new double[peaks.size()];
        //    //		double[] y2 = new double[onsets.length];
        //    //		Iterator<Integer> it = peaks.iterator();
        //    //		onsetList = new EventList();
        //    //		double minSalience = Peaks.min(spectralFlux);
        //    //		for (int i = 0; i < onsets.length; i++) {
        //    //			int index = it.next();
        //    //			onsets[i] = index * hop;
        //    //			y2[i] = spectralFlux[index];
        //    //			Event e = BeatTrackDisplay.newBeat(onsets[i], 0);
        //    ////			if (debug)
        //    ////				System.err.printf("Onset: %8.3f  %8.3f  %8.3f\n",
        //    ////						onsets[i], energy[index], slope[index]);
        //    ////			e.salience = slope[index];	// or combination of energy + slope??
        //    //			// Note that salience must be non-negative or the beat tracking system fails!
        //    //			e.salience = spectralFlux[index] - minSalience;
        //    //			onsetList.add(e);
        //    //		}
        //    double p1 = 0.35;
        //    double p2 = 0.84;

        //    Peaks.normalise(spectralFlux);
        //    findOnsets(p1, p2);

        //    if (progressCallback != null)
        //        progressCallback.setFraction(1.0);
        //    //if (doOnsetPlot)
        //    //{
        //    //    double[] x1 = new double[spectralFlux.length];
        //    //    for (int i = 0; i < x1.length; i++)
        //    //        x1[i] = i * hopTime;
        //    //    plot.addPlot(x1, spectralFlux, Color.red, 4);
        //    //    plot.addPlot(onsets, y2Onsets, Color.green, 3);
        //    //    plot.setTitle("Spectral flux and onsets");
        //    //    plot.fitAxes();
        //    //}
        //    if (debug)
        //    {
        //        System.err.printf("Onsets: %d\nContinue? ", onsets.Length);
        //        readLine();
        //    }
        //} // processFile()

        public void findOnsets(double p1, double p2)
        {
            List<int> peaks = Peaks.findPeaks(spectralFlux, (int) Math.Round(0.06/hopTime), p1, p2, true);
            onsets = new double[peaks.Count];
            y2Onsets = new double[onsets.Length];
            onsetList = new List<Event>();
            double minSalience = Peaks.min(spectralFlux);
            for (int i = 0; i < onsets.Length; i++)
            {
                int index = peaks[i];
                onsets[i] = index*hopTime;
                y2Onsets[i] = spectralFlux[index];
                var e = new Event(onsets[i], onsets[i], onsets[i], 56, 64, 0, 0, 1);
                //			if (debug)
                //				System.err.printf("Onset: %8.3f  %8.3f  %8.3f\n",
                //						onsets[i], energy[index], slope[index]);
                //			e.salience = slope[index];	// or combination of energy + slope??
                // Note that salience must be non-negative or the beat tracking system fails!
                e.salience = spectralFlux[index] - minSalience;
                onsetList.Add(e);
            }
        }

        /** Reads a text file containing a list of whitespace-separated feature values.
         *  Created for paper submitted to ICASSP'07.
         *  @param fileName File containing the data
         *  @return An array containing the feature values
         */
        //public static double[] getFeatures(string fileName)
        //{
        //    ArrayList<Double> l = new ArrayList<Double>();
        //    try
        //    {
        //        BufferedReader b = new BufferedReader(new FileReader(fileName));
        //        while (true)
        //        {
        //            String s = b.readLine();
        //            if (s == null)
        //                break;
        //            int start = 0;
        //            while (start < s.length())
        //            {
        //                int len = s.substring(start).indexOf(' ');
        //                String t = null;
        //                if (len < 0)
        //                    t = s.substring(start);
        //                else if (len > 0)
        //                {
        //                    t = s.substring(start, start + len);
        //                }
        //                if (t != null)
        //                    try
        //                    {
        //                        l.add(Double.parseDouble(t));
        //                    }
        //                    catch (NumberFormatException e)
        //                    {
        //                        System.err.println(e);
        //                        if (l.size() == 0)
        //                            l.add(new Double(0));
        //                        else
        //                            l.add(new Double(l.get(l.size() - 1)));
        //                    }
        //                start += len + 1;
        //                if (len < 0)
        //                    break;
        //            }
        //        }
        //        double[] features = new double[l.size()];
        //        Iterator<Double> it = l.iterator();
        //        for (int i = 0; it.hasNext(); i++)
        //            features[i] = it.next().doubleValue();
        //        return features;
        //    }
        //    catch (FileNotFoundException e)
        //    {
        //        e.printStackTrace();
        //        return null;
        //    }
        //    catch (IOException e)
        //    {
        //        e.printStackTrace();
        //        return null;
        //    }
        //    catch (NumberFormatException e)
        //    {
        //        e.printStackTrace();
        //        return null;
        //    }
        //} // getFeatures()

        /** Reads a file of feature values, treated as an onset detection function,
         *  and finds peaks, which are stored in <code>onsetList</code> and <code>onsets</code>.
         * @param fileName The file of feature values
         * @param hopTime The spacing of feature values in time
         */
        //public void processFeatures(string fileName, double hopTime)
        //{
        //    double hop = hopTime;
        //    double[] features = getFeatures(fileName);
        //    Peaks.normalise(features);
        //    LinkedList<Integer> peaks = Peaks.findPeaks(features, (int)Math.round(0.06 / hop), 0.35, 0.84, true);
        //    onsets = new double[peaks.size()];
        //    double[] y2 = new double[onsets.length];
        //    Iterator<Integer> it = peaks.iterator();
        //    onsetList = new EventList();
        //    double minSalience = Peaks.min(features);
        //    for (int i = 0; i < onsets.length; i++)
        //    {
        //        int index = it.next();
        //        onsets[i] = index * hop;
        //        y2[i] = features[index];
        //        Event e = BeatTrackDisplay.newBeat(onsets[i], 0);
        //        e.salience = features[index] - minSalience;
        //        onsetList.add(e);
        //    }
        //} // processFeatures()

        /** Copies output of audio processing to the display panel. */
        //    public void setDisplay(BeatTrackDisplay btd) {
        //    int energy2[] = new int[totalFrames*energyOversampleFactor];
        //    double time[] = new double[totalFrames*energyOversampleFactor];
        //    for (int i = 0; i < totalFrames*energyOversampleFactor; i++) {
        //        energy2[i] = (int) (energy[i] * 4 * energyOversampleFactor);
        //        time[i] = i * hopTime / energyOversampleFactor;
        //    }
        //    btd.setMagnitudes(energy2);
        //    btd.setEnvTimes(time);
        //    btd.setSpectro(frames, totalFrames, hopTime, 0);//fftTime/hopTime);
        //    btd.setOnsets(onsets);
        //    btd.setOnsetList(onsetList);
        //} // setDisplay()
    } // class AudioProcessor
}