namespace DJPad.Sources
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Formats.Cda;

    public class CdSource : IFileSource
    {
        #region Fields

        private readonly Win32Functions.CDROM_TOC cdToc = new Win32Functions.CDROM_TOC();

        private readonly int trackNumber;

        private IntPtr cdHandle;

        private int lastAt;

        private TimeSpan lengthOfTrack;

        private int startAt;

        private int stopAt;

        #endregion

        #region Constructors and Destructors

        public CdSource()
        {
        }

        public CdSource(char driveLetter, int trackNumber)
        {
            this.trackNumber = trackNumber;
            this.InitializeDrive(driveLetter);
            this.InitializeTrack(trackNumber);
        }

        #endregion

        #region Public Methods and Operators

        public string GetFileType()
        {
            return ".CDA";
        }

        public FormatInformation GetFormat()
        {
            return new FormatInformation { BytesPerSample = 2, Channels = 2, SampleRate = 44100 };
        }

        public Sample GetSample(int dataLength)
        {
            var s = new Sample();

            byte[] cdData = this.ReadSector(this.lastAt, Win32Functions.NSECTORS);

            // No more data from the Cd.
            if (cdData != null)
            {
                s.Data = cdData;
                s.DataLength = cdData.Length;
                this.lastAt += Win32Functions.NSECTORS;
            }
            else
            {
                s.Data = new byte[0];
                s.DataLength = 0;
            }

            return s;
        }

        public void Load(string filename)
        {
            char driveLetter = filename.ToCharArray()[0];
            int trackNumber = int.Parse(Path.GetFileNameWithoutExtension(filename).Replace("Track", string.Empty));

            this.InitializeDrive(driveLetter);
            this.InitializeTrack(trackNumber);
        }

        public void Close()
        {
            if (this.cdHandle != IntPtr.Zero)
            {
                Win32Functions.CloseHandle(this.cdHandle);
            }
        }

        public void InitializeDrive(char driveLetter)
        {
            this.cdHandle = Win32Functions.CreateFile(
                "\\\\.\\" + driveLetter + ':',
                Win32Functions.GENERIC_READ,
                Win32Functions.FILE_SHARE_READ,
                IntPtr.Zero,
                Win32Functions.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if ((int)this.cdHandle != -1)
            {
                uint BytesRead = 0;
                int retVal = Win32Functions.DeviceIoControl(
                    this.cdHandle,
                    Win32Functions.IOCTL_CDROM_READ_TOC,
                    IntPtr.Zero,
                    0,
                    this.cdToc,
                    (uint)Marshal.SizeOf(this.cdToc),
                    ref BytesRead,
                    IntPtr.Zero);
            }
        }

        public void InitializeTrack(int trackNumber)
        {
            if ((trackNumber >= this.cdToc.FirstTrack) && (trackNumber <= this.cdToc.LastTrack))
            {
                this.startAt = this.cdToc.TrackData[trackNumber - 1].MSF2LBA();
                this.stopAt = this.cdToc.TrackData[trackNumber].MSF2LBA() - 1;
                this.lastAt = this.startAt;

                this.lengthOfTrack = TimeSpan.FromSeconds(this.cdToc.TrackData[trackNumber].MSF2LBA() / 75);

                // this.LockCD();
            }
        }

        public bool LockCD()
        {
            if (((int)this.cdHandle != -1) && ((int)this.cdHandle != 0))
            {
                uint Dummy = 0;
                var pmr = new Win32Functions.PREVENT_MEDIA_REMOVAL();
                pmr.PreventMediaRemoval = 1;
                return Win32Functions.DeviceIoControl(
                    this.cdHandle,
                    Win32Functions.IOCTL_STORAGE_MEDIA_REMOVAL,
                    pmr,
                    (uint)Marshal.SizeOf(pmr),
                    IntPtr.Zero,
                    0,
                    ref Dummy,
                    IntPtr.Zero) != 0;
            }
         
            return false;
        }

        #endregion

        #region Methods

        protected byte[] ReadSector(int sector, int NumSectors)
        {
            if ((sector + NumSectors) <= this.stopAt)
            {
                var buffer = new byte[Win32Functions.CB_AUDIO * NumSectors];

                var rri = new Win32Functions.RAW_READ_INFO();
                rri.TrackMode = Win32Functions.TRACK_MODE_TYPE.CDDA;
                rri.SectorCount = (uint)NumSectors;
                rri.DiskOffset = sector * Win32Functions.CB_CDROMSECTOR;

                uint BytesRead = 0;
                Win32Functions.DeviceIoControl(
                    this.cdHandle,
                    Win32Functions.IOCTL_CDROM_RAW_READ,
                    rri,
                    (uint)Marshal.SizeOf(rri),
                    buffer,
                    (uint)NumSectors * Win32Functions.CB_AUDIO,
                    ref BytesRead,
                    IntPtr.Zero);

                return buffer;
            }

            return null;
        }

        #endregion

        public string FileName { get; set; }

        public IMetadata GetMetadata()
        {
            return new SimpleMetadataSource()
                       {
                           Album = "Unknown",
                           Artist = "Unknown",
                           Title = string.Format("Track# {0}", this.trackNumber)
                       };
        }


        public TimeSpan Duration
        {
            get { return this.lengthOfTrack; }
        }

        public bool EndOfFile
        {
            get
            {
                return this.stopAt <= this.lastAt;
            }
        }

        public TimeSpan Position
        {
            set
            {
               
            }
            get
            {
                return TimeSpan.FromSeconds(this.stopAt - this.stopAt / 75);
            }
        }
    }
}