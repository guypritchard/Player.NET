namespace Support
{
    using System;
    using System.IO;

    internal interface IThreadRunnable
    {
        #region Public Methods and Operators

        void Run();

        #endregion
    }

    internal class SupportClass
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Method that copies an array of sbytes from a String to a received array .
        /// </summary>
        /// <param name="sourceString">The String to get the sbytes.</param>
        /// <param name="sourceStart">Position in the String to start getting sbytes.</param>
        /// <param name="sourceEnd">Position in the String to end getting sbytes.</param>
        /// <param name="destinationArray">Array to store the bytes.</param>
        /// <param name="destinationStart">Position in the destination array to start storing the sbytes.</param>
        /// <returns>An array of sbytes</returns>
        public static void GetSBytesFromString(
            string sourceString, int sourceStart, int sourceEnd, ref sbyte[] destinationArray, int destinationStart)
        {
            int sourceCounter = sourceStart;
            int destinationCounter = destinationStart;
            while (sourceCounter < sourceEnd)
            {
                destinationArray[destinationCounter] = (sbyte)sourceString[sourceCounter];
                sourceCounter++;
                destinationCounter++;
            }
        }

        /// <summary>Reads a number of characters from the current source Stream and writes the data to the target array at the specified index.</summary>
        /// <param name="sourceStream">The source Stream to read from</param>
        /// <param name="target">Contains the array of characteres read from the source Stream.</param>
        /// <param name="start">The starting index of the target array.</param>
        /// <param name="count">The maximum number of characters to read from the source Stream.</param>
        /// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source Stream.</returns>
        public static Int32 ReadInput(Stream sourceStream, ref sbyte[] target, int start, int count)
        {
            var receiver = new byte[target.Length];
            int bytesRead = sourceStream.Read(receiver, start, count);

            for (int i = start; i < start + bytesRead; i++)
            {
                target[i] = (sbyte)receiver[i];
            }

            return bytesRead;
        }

        /*******************************/

        /// <summary>
        ///     Converts an array of sbytes to an array of bytes
        /// </summary>
        /// <param name="sbyteArray">The array of sbytes to be converted</param>
        /// <returns>The new array of bytes</returns>
        public static byte[] ToByteArray(sbyte[] sbyteArray)
        {
            var byteArray = new byte[sbyteArray.Length];
            for (int index = 0; index < sbyteArray.Length; index++)
            {
                byteArray[index] = (byte)sbyteArray[index];
            }
            return byteArray;
        }

        public static int UrShift(int number, int bits)
        {
            if (number >= 0)
            {
                return number >> bits;
            }

            return (number >> bits) + (2 << ~bits);
        }

        public static int UrShift(int number, long bits)
        {
            return UrShift(number, (int)bits);
        }

        public static long UrShift(long number, int bits)
        {
            if (number >= 0)
            {
                return number >> bits;
            }

            return (number >> bits) + (2L << ~bits);
        }

        public static long UrShift(long number, long bits)
        {
            return UrShift(number, (int)bits);
        }

        #endregion

        /*******************************/

        internal class RandomAccessFileSupport
        {
            #region Public Methods and Operators

            public static FileStream CreateRandomAccessFile(string fileName, string mode)
            {
                if (string.Compare(mode, "rw", StringComparison.Ordinal) == 0)
                {
                    return new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }

                if (string.Compare(mode, "r", StringComparison.Ordinal) == 0)
                {
                    return new FileStream(fileName, FileMode.Open, FileAccess.Read);
                }

                throw new ArgumentException("mode");
            }

            public static FileStream CreateRandomAccessFile(FileInfo fileName, string mode)
            {
                return CreateRandomAccessFile(fileName.FullName, mode);
            }

            public static void WriteBytes(string data, FileStream fileStream)
            {
                int index = 0;
                int length = data.Length;

                while (index < length)
                {
                    fileStream.WriteByte((byte)data[index++]);
                }
            }

            public static void WriteChars(string data, FileStream fileStream)
            {
                WriteBytes(data, fileStream);
            }

            public static void WriteRandomFile(sbyte[] sByteArray, FileStream fileStream)
            {
                byte[] byteArray = ToByteArray(sByteArray);
                fileStream.Write(byteArray, 0, byteArray.Length);
            }

            #endregion
        }

        public static class LittleEndian
        {
            public static int ToInt16(byte[] value, int startIndex)
            {
                return (int)LittleEndianFromBytes(value, startIndex, 2);
            }

            public static int ToInt32(byte[] value, int startIndex, bool littleEndian = false)
            {
                return (int)LittleEndianFromBytes(value, startIndex, 4);
            }

            public static long ToInt64(byte[] value, int startIndex, bool littleEndian = false)
            {
                return LittleEndianFromBytes(value, startIndex, 2);
            }

            private static long LittleEndianFromBytes(byte[] buffer, int startIndex, int len)
            {
                long ret = 0;
                for (int i = 0; i < len; i++)
                {
                    ret = unchecked((ret << 8) | buffer[startIndex + len - 1 - i]);
                }
                return ret;
            }
        }

        public static class BigEndian
        {
            public static int ToInt16(byte[] value, int startIndex, bool littleEndian = false)
            {
                return (int)BigEndianFromBytes(value, startIndex, 2);
            }

            public static int ToInt32(byte[] value, int startIndex, bool littleEndian = false)
            {
                return (int)BigEndianFromBytes(value, startIndex, 4);
            }

            public static long ToInt64(byte[] value, int startIndex, bool littleEndian = false)
            {
                return BigEndianFromBytes(value, startIndex, 2);
            }

            private static long BigEndianFromBytes(byte[] buffer, int startIndex, int len)
            {
                long ret = 0;
                for (int i = 0; i < len; i++)
                {
                    ret = unchecked((ret << 8) | buffer[startIndex + i]);
                }
                return ret;
            }
        }
    }
}