//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE.
//
//  This material may not be duplicated in whole or in part, except for 
//  personal use, without the express written consent of the author. 
//
//  Email:  ianier@hotmail.com
//
//  Copyright (C) 1999-2003 Ianier Munoz. All Rights Reserved.

namespace DJPad.Output.Wave
{
    using System;
    using System.Runtime.InteropServices;
    using DJPad.Core;

    internal class WaveNative
    {
        #region Constants

        public const int CALLBACK_FUNCTION = 0x00030000; // dwCallback is a FARPROC 

        public const int MMSYSERR_NOERROR = 0; // no error

        public const int MM_WIM_CLOSE = 0x3BF;

        public const int MM_WIM_DATA = 0x3C0;

        public const int MM_WIM_OPEN = 0x3BE;

        public const int MM_WOM_CLOSE = 0x3BC;

        public const int MM_WOM_DONE = 0x3BD;

        public const int MM_WOM_OPEN = 0x3BB;

        public const int TIME_BYTES = 0x0004; // current byte offset 

        public const int TIME_MS = 0x0001; // time in milliseconds 

        public const int TIME_SAMPLES = 0x0002; // number of wave samples 

        private const string mmdll = "winmm.dll";

        #endregion

        #region Delegates

        public delegate void WaveDelegate(IntPtr hdrvr, int uMsg, int dwUser, ref NativeWaveHeader wavhdr, int dwParam2);

        #endregion

        #region Public Methods and Operators

        [DllImport(mmdll)]
        public static extern int waveInAddBuffer(IntPtr hwi, ref NativeWaveHeader pwh, int cbwh);

        [DllImport(mmdll)]
        public static extern int waveInClose(IntPtr hwi);

        [DllImport(mmdll)]
        public static extern int waveInGetNumDevs();

        [DllImport(mmdll)]
        public static extern int waveInOpen(
            out IntPtr phwi, int uDeviceID, WaveFormat lpFormat, WaveDelegate dwCallback, int dwInstance, int dwFlags);

        [DllImport(mmdll)]
        public static extern int waveInPrepareHeader(IntPtr hWaveIn, ref NativeWaveHeader lpWaveInHdr, int uSize);

        [DllImport(mmdll)]
        public static extern int waveInReset(IntPtr hwi);

        [DllImport(mmdll)]
        public static extern int waveInStart(IntPtr hwi);

        [DllImport(mmdll)]
        public static extern int waveInStop(IntPtr hwi);

        [DllImport(mmdll)]
        public static extern int waveInUnprepareHeader(IntPtr hWaveIn, ref NativeWaveHeader lpWaveInHdr, int uSize);

        [DllImport(mmdll)]
        public static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport(mmdll)]
        public static extern int waveOutGetNumDevs();

        [DllImport(mmdll)]
        public static extern int waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);

        [DllImport(mmdll)]
        public static extern int waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);

        [DllImport(mmdll)]
        public static extern int waveOutOpen(
            out IntPtr hWaveOut,
            int uDeviceID,
            WaveFormat lpFormat,
            WaveDelegate dwCallback,
            int dwInstance,
            int dwFlags);

        [DllImport(mmdll)]
        public static extern int waveOutPause(IntPtr hWaveOut);

        [DllImport(mmdll)]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref NativeWaveHeader lpWaveOutHdr, int uSize);

        [DllImport(mmdll)]
        public static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport(mmdll)]
        public static extern int waveOutRestart(IntPtr hWaveOut);

        [DllImport(mmdll)]
        public static extern int waveOutSetVolume(IntPtr hWaveOut, int dwVolume);

        [DllImport(mmdll)]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref NativeWaveHeader lpWaveOutHdr, int uSize);

        [DllImport(mmdll)]
        public static extern int waveOutWrite(IntPtr hWaveOut, ref NativeWaveHeader lpWaveOutHdr, int uSize);

        #endregion

        // consts

        // WaveOut calls

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeWaveHeader
        {
            public IntPtr lpData; // pointer to locked data buffer

            public int dwBufferLength; // length of data buffer

            public int dwBytesRecorded; // used for input only

            public IntPtr dwUser; // for client's use

            public int dwFlags; // assorted flags (see defines)

            public int dwLoops; // loop control counter

            public IntPtr lpNext; // PWaveHdr, reserved for driver

            public int reserved; // reserved for driver
        }
    }
}