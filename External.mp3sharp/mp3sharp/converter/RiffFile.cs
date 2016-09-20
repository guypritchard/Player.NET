/*
* 02/23/99 JavaConversion by E.B, JavaLayer
*/
/*===========================================================================

riff.h  -  Don Cross, April 1993.

RIFF file format classes.
See Chapter 8 of "Multimedia Programmer's Reference" in
the Microsoft Windows SDK.

See also:
..\source\riff.cpp
ddc.h

===========================================================================*/

namespace javazoom.jl.converter
{
    using System;
    using System.IO;

    using Support;

    /// <summary>
    ///     Class to manage RIFF files
    /// </summary>
    internal class RiffFile
    {
        #region Constants

        public const int DDC_FAILURE = 1; // The operation failed for unspecified reasons

        public const int DDC_FILE_ERROR = 3; // Operation encountered file I/O error

        public const int DDC_INVALID_CALL = 4; // Operation was called with invalid parameters

        public const int DDC_INVALID_FILE = 6; // File format does not match

        public const int DDC_OUT_OF_MEMORY = 2; // Operation failed due to running out of memory

        public const int DDC_SUCCESS = 0; // The operation succeded

        public const int DDC_USER_ABORT = 5; // Operation was aborted by the user

        public const int RFM_READ = 2; // open for read

        // RiffFileMode
        public const int RFM_UNKNOWN = 0; // undefined type (can use to mean "N/A" or "not open")

        public const int RFM_WRITE = 1; // open for write

        #endregion

        #region Fields

        private readonly RiffChunkHeader riff_header; // header for whole file

        protected internal Stream file; // I/O stream to use

        protected internal int fmode; // current file I/O mode

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public RiffFile()
        {
            this.file = null;
            this.fmode = RFM_UNKNOWN;
            this.riff_header = new RiffChunkHeader(this);

            this.riff_header.ckID = FourCC("RIFF");
            this.riff_header.ckSize = 0;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Fill the header.
        /// </summary>
        public static int FourCC(string chunkName)
        {
            var p = new[] { (sbyte)(0x20), (sbyte)(0x20), (sbyte)(0x20), (sbyte)(0x20) };
            SupportClass.GetSBytesFromString(chunkName, 0, 4, ref p, 0);
            var ret =
                (int)
                (((p[0] << 24) & 0xFF000000) | ((p[1] << 16) & 0x00FF0000) | ((p[2] << 8) & 0x0000FF00)
                 | (p[3] & 0x000000FF));
            return ret;
        }

        /// <summary>
        ///     Write Data to specified offset.
        /// </summary>
        public virtual int Backpatch(long FileOffset, RiffChunkHeader Data, int NumBytes)
        {
            if (this.file == null)
            {
                return DDC_INVALID_CALL;
            }
            try
            {
                this.file.Seek(FileOffset, SeekOrigin.Begin);
            }
            catch (IOException ioe)
            {
                return DDC_FILE_ERROR;
            }
            return Write(Data, NumBytes);
        }

        public virtual int Backpatch(long FileOffset, sbyte[] Data, int NumBytes)
        {
            if (this.file == null)
            {
                return DDC_INVALID_CALL;
            }
            try
            {
                this.file.Seek(FileOffset, SeekOrigin.Begin);
            }
            catch (IOException ioe)
            {
                return DDC_FILE_ERROR;
            }
            return Write(Data, NumBytes);
        }

        /// <summary>
        ///     Close Riff File.
        ///     Length is written too.
        /// </summary>
        public virtual int Close()
        {
            int retcode = DDC_SUCCESS;

            switch (this.fmode)
            {
                case RFM_WRITE:
                    try
                    {
                        this.file.Seek(0, SeekOrigin.Begin);
                        try
                        {
                            var br = new sbyte[8];
                            br[0] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 24)) & 0x000000FF);
                            br[1] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 16)) & 0x000000FF);
                            br[2] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 8)) & 0x000000FF);
                            br[3] = (sbyte)(this.riff_header.ckID & 0x000000FF);

                            br[7] = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 24)) & 0x000000FF);
                            br[6] = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 16)) & 0x000000FF);
                            br[5] = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 8)) & 0x000000FF);
                            br[4] = (sbyte)(this.riff_header.ckSize & 0x000000FF);
                            this.file.Write(SupportClass.ToByteArray(br), 0, 8);
                            this.file.Close();
                        }
                        catch (IOException ioe)
                        {
                            retcode = DDC_FILE_ERROR;
                        }
                    }
                    catch (IOException ioe)
                    {
                        retcode = DDC_FILE_ERROR;
                    }
                    break;

                case RFM_READ:
                    try
                    {
                        this.file.Close();
                    }
                    catch (IOException ioe)
                    {
                        retcode = DDC_FILE_ERROR;
                    }
                    break;
            }
            this.file = null;
            this.fmode = RFM_UNKNOWN;
            return retcode;
        }

        /// <summary>
        ///     Return File Mode.
        /// </summary>
        public virtual int CurrentFileMode()
        {
            return this.fmode;
        }

        /// <summary>
        ///     Return File Position.
        /// </summary>
        public virtual long CurrentFilePosition()
        {
            long position;
            try
            {
                position = this.file.Position;
            }
            catch (IOException ioe)
            {
                position = - 1;
            }
            return position;
        }

        /// <summary>
        ///     Expect NumBytes data.
        /// </summary>
        public virtual int Expect(String Data, int NumBytes)
        {
            sbyte target = 0;
            int cnt = 0;
            try
            {
                while ((NumBytes--) != 0)
                {
                    target = (sbyte)this.file.ReadByte();
                    if (target != Data[cnt++])
                    {
                        return DDC_FILE_ERROR;
                    }
                }
            }
            catch (IOException ioe)
            {
                return DDC_FILE_ERROR;
            }
            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Open a RIFF file.
        /// </summary>
        public virtual int Open(String Filename, int NewMode)
        {
            int retcode = DDC_SUCCESS;

            if (this.fmode != RFM_UNKNOWN)
            {
                retcode = this.Close();
            }

            if (retcode == DDC_SUCCESS)
            {
                switch (NewMode)
                {
                    case RFM_WRITE:
                        try
                        {
                            this.file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "rw");

                            try
                            {
                                // Write the RIFF header...
                                // We will have to come back later and patch it!
                                var br = new sbyte[8];
                                br[0] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 24)) & 0x000000FF);
                                br[1] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 16)) & 0x000000FF);
                                br[2] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 8)) & 0x000000FF);
                                br[3] = (sbyte)(this.riff_header.ckID & 0x000000FF);

                                var br4 = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 24)) & 0x000000FF);
                                var br5 = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 16)) & 0x000000FF);
                                var br6 = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 8)) & 0x000000FF);
                                var br7 = (sbyte)(this.riff_header.ckSize & 0x000000FF);

                                br[4] = br7;
                                br[5] = br6;
                                br[6] = br5;
                                br[7] = br4;

                                this.file.Write(SupportClass.ToByteArray(br), 0, 8);
                                this.fmode = RFM_WRITE;
                            }
                            catch (IOException ioe)
                            {
                                this.file.Close();
                                this.fmode = RFM_UNKNOWN;
                            }
                        }
                        catch (IOException ioe)
                        {
                            this.fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }
                        break;

                    case RFM_READ:
                        try
                        {
                            this.file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "r");
                            try
                            {
                                // Try to read the RIFF header...   				   
                                var br = new sbyte[8];
                                SupportClass.ReadInput(this.file, ref br, 0, 8);
                                this.fmode = RFM_READ;
                                this.riff_header.ckID = (int)((br[0] << 24) & 0xFF000000) | 
                                                             ((br[1] << 16) & 0x00FF0000) | 
                                                             ((br[2] << 8)  & 0x0000FF00) | 
                                                              (br[3]        & 0x000000FF);
                                
                                this.riff_header.ckSize = (int)((br[4] << 24) & 0xFF000000) | 
                                                               ((br[5] << 16) & 0x00FF0000) | 
                                                               ((br[6] << 8)  & 0x0000FF00) | 
                                                                (br[7]        & 0x000000FF);
                            }
                            catch (IOException)
                            {
                                this.file.Close();
                                this.fmode = RFM_UNKNOWN;
                            }
                        }
                        catch (IOException)
                        {
                            this.fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }
                        break;

                    default:
                        retcode = DDC_INVALID_CALL;
                        break;
                }
            }
            return retcode;
        }

        /// <summary>
        ///     Open a RIFF STREAM.
        /// </summary>
        public virtual int Open(Stream stream, int newMode)
        {
            int retcode = DDC_SUCCESS;

            if (this.fmode != RFM_UNKNOWN)
            {
                retcode = this.Close();
            }

            if (retcode == DDC_SUCCESS)
            {
                switch (newMode)
                {
                    case RFM_WRITE:
                        try
                        {
                            this.file = stream;

                            try
                            {
                                // Write the RIFF header...
                                // We will have to come back later and patch it!
                                var br = new sbyte[8];
                                br[0] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 24)) & 0x000000FF);
                                br[1] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 16)) & 0x000000FF);
                                br[2] = (sbyte)((SupportClass.UrShift(this.riff_header.ckID, 8)) & 0x000000FF);
                                br[3] = (sbyte)(this.riff_header.ckID & 0x000000FF);

                                var br4 = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 24)) & 0x000000FF);
                                var br5 = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 16)) & 0x000000FF);
                                var br6 = (sbyte)((SupportClass.UrShift(this.riff_header.ckSize, 8)) & 0x000000FF);
                                var br7 = (sbyte)(this.riff_header.ckSize & 0x000000FF);

                                br[4] = br7;
                                br[5] = br6;
                                br[6] = br5;
                                br[7] = br4;

                                this.file.Write(SupportClass.ToByteArray(br), 0, 8);
                                this.fmode = RFM_WRITE;
                            }
                            catch (IOException ioe)
                            {
                                this.file.Close();
                                this.fmode = RFM_UNKNOWN;
                            }
                        }
                        catch (IOException ioe)
                        {
                            this.fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }
                        break;

                    case RFM_READ:
                        try
                        {
                            this.file = stream;
                            try
                            {
                                // Try to read the RIFF header...   				   
                                var br = new sbyte[8];
                                SupportClass.ReadInput(this.file, ref br, 0, 8);
                                this.fmode = RFM_READ;
                                this.riff_header.ckID = (int)((br[0] << 24) & 0xFF000000) | ((br[1] << 16) & 0x00FF0000)
                                                        | ((br[2] << 8) & 0x0000FF00) | (br[3] & 0x000000FF);
                                this.riff_header.ckSize = (int)((br[4] << 24) & 0xFF000000)
                                                          | ((br[5] << 16) & 0x00FF0000) | ((br[6] << 8) & 0x0000FF00)
                                                          | (br[7] & 0x000000FF);
                            }
                            catch (IOException ioe)
                            {
                                this.file.Close();
                                this.fmode = RFM_UNKNOWN;
                            }
                        }
                        catch (IOException ioe)
                        {
                            this.fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }
                        break;

                    default:
                        retcode = DDC_INVALID_CALL;
                        break;
                }
            }
            return retcode;
        }

        /// <summary>
        ///     Read NumBytes data.
        /// </summary>
        public virtual int Read(sbyte[] Data, int NumBytes)
        {
            int retcode = DDC_SUCCESS;
            try
            {
                SupportClass.ReadInput(this.file, ref Data, 0, NumBytes);
            }
            catch (IOException ioe)
            {
                retcode = DDC_FILE_ERROR;
            }
            return retcode;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(sbyte[] Data, int NumBytes)
        {
            if (this.fmode != RFM_WRITE)
            {
                return DDC_INVALID_CALL;
            }
            
            try
            {
                this.file.Write(SupportClass.ToByteArray(Data), 0, NumBytes);
                this.fmode = RFM_WRITE;
            }
            catch (IOException)
            {
                return DDC_FILE_ERROR;
            }

            this.riff_header.ckSize += NumBytes;
            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(short[] Data, int NumBytes)
        {
            var theData = new sbyte[NumBytes];
            int yc = 0;
            for (int y = 0; y < NumBytes; y = y + 2)
            {
                theData[y] = (sbyte)(Data[yc] & 0x00FF);
                theData[y + 1] = (sbyte)((SupportClass.UrShift(Data[yc++], 8)) & 0x00FF);
            }

            if (this.fmode != RFM_WRITE)
            {
                return DDC_INVALID_CALL;
            }
            
            try
            {
                this.file.Write(SupportClass.ToByteArray(theData), 0, NumBytes);
                this.fmode = RFM_WRITE;
            }
            catch (IOException)
            {
                return DDC_FILE_ERROR;
            }
            
            this.riff_header.ckSize += NumBytes;
            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(RiffChunkHeader Triff_header, int NumBytes)
        {
            var br = new sbyte[8];
            br[0] = (sbyte)((SupportClass.UrShift(Triff_header.ckID, 24)) & 0x000000FF);
            br[1] = (sbyte)((SupportClass.UrShift(Triff_header.ckID, 16)) & 0x000000FF);
            br[2] = (sbyte)((SupportClass.UrShift(Triff_header.ckID, 8)) & 0x000000FF);
            br[3] = (sbyte)(Triff_header.ckID & 0x000000FF);

            var br4 = (sbyte)((SupportClass.UrShift(Triff_header.ckSize, 24)) & 0x000000FF);
            var br5 = (sbyte)((SupportClass.UrShift(Triff_header.ckSize, 16)) & 0x000000FF);
            var br6 = (sbyte)((SupportClass.UrShift(Triff_header.ckSize, 8)) & 0x000000FF);
            var br7 = (sbyte)(Triff_header.ckSize & 0x000000FF);

            br[4] = br7;
            br[5] = br6;
            br[6] = br5;
            br[7] = br4;

            if (this.fmode != RFM_WRITE)
            {
                return DDC_INVALID_CALL;
            }
            try
            {
                this.file.Write(SupportClass.ToByteArray(br), 0, NumBytes);
                this.fmode = RFM_WRITE;
            }
            catch (IOException ioe)
            {
                return DDC_FILE_ERROR;
            }
            this.riff_header.ckSize += NumBytes;
            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(short Data, int NumBytes)
        {
            short theData = Data; //(short) (((SupportClass.URShift(Data, 8)) & 0x00FF) | ((Data << 8) & 0xFF00));
            if (this.fmode != RFM_WRITE)
            {
                return DDC_INVALID_CALL;
            }
            try
            {
                BinaryWriter temp_BinaryWriter;
                temp_BinaryWriter = new BinaryWriter(this.file);
                temp_BinaryWriter.Write(theData);
                this.fmode = RFM_WRITE;
            }
            catch (IOException ioe)
            {
                return DDC_FILE_ERROR;
            }
            this.riff_header.ckSize += NumBytes;
            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(int Data, int NumBytes)
        {
            var theDataL = (short)((SupportClass.UrShift(Data, 16)) & 0x0000FFFF);
            var theDataR = (short)(Data & 0x0000FFFF);
            var theDataLI = (short)(((SupportClass.UrShift(theDataL, 8)) & 0x00FF) | ((theDataL << 8) & 0xFF00));
            var theDataRI = (short)(((SupportClass.UrShift(theDataR, 8)) & 0x00FF) | ((theDataR << 8) & 0xFF00));
            int theData = Data;
            //((theDataRI << 16) & (int) SupportClass.Identity(0xFFFF0000)) | (theDataLI & 0x0000FFFF);
            if (this.fmode != RFM_WRITE)
            {
                return DDC_INVALID_CALL;
            }
            try
            {
                BinaryWriter temp_BinaryWriter;
                temp_BinaryWriter = new BinaryWriter(this.file);
                temp_BinaryWriter.Write(theData);
                this.fmode = RFM_WRITE;
            }
            catch (IOException ioe)
            {
                return DDC_FILE_ERROR;
            }
            this.riff_header.ckSize += NumBytes;
            return DDC_SUCCESS;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Seek in the File.
        /// </summary>
        protected internal virtual int Seek(long offset)
        {
            int rc;
            try
            {
                this.file.Seek(offset, SeekOrigin.Begin);
                rc = DDC_SUCCESS;
            }
            catch (IOException ioe)
            {
                rc = DDC_FILE_ERROR;
            }
            return rc;
        }

        /// <summary>
        ///     Error Messages.
        /// </summary>
        private string DDCRETString(int retCode)
        {
            switch (retCode)
            {
                case DDC_SUCCESS:
                    return "DDC_SUCCESS";

                case DDC_FAILURE:
                    return "DDC_FAILURE";

                case DDC_OUT_OF_MEMORY:
                    return "DDC_OUT_OF_MEMORY";

                case DDC_FILE_ERROR:
                    return "DDC_FILE_ERROR";

                case DDC_INVALID_CALL:
                    return "DDC_INVALID_CALL";

                case DDC_USER_ABORT:
                    return "DDC_USER_ABORT";

                case DDC_INVALID_FILE:
                    return "DDC_INVALID_FILE";
            }

            return "Unknown Error";
        }

        #endregion

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'RiffChunkHeader' to access its enclosing instance. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1019"'

        // DDCRET
    }
}