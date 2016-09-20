using DJPad.Core.Interfaces;
using DJPad.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsMediaLib;

namespace DJPad.Sources
{
    public class WmaMetadataSource : IMetadata
    {
        private readonly IWMHeaderInfo wmaHeader;
        private readonly string fileName;

        public WmaMetadataSource(IWMHeaderInfo3 wmaHeader, string fileName)
        {
            this.wmaHeader = wmaHeader; 
            this.fileName = fileName;
        }

        public string Album
        {
            get 
            {
                return this.GetStringValue(Constants.g_wszWMAlbumTitle);
            }
        }

        public string Artist
        {
            get 
            {
                return this.GetStringValue(Constants.g_wszWMAlbumArtist);
            }
        }

        public string Title
        {
            get 
            { 
                return this.GetStringValue(Constants.g_wszWMTitle); 
            }
        }

        public Bitmap AlbumArt
        {
            get 
            {
                var art = AlternateArtSource.FileSourceArt(Path.GetDirectoryName(this.fileName));

                if (art != null && art.Length > 0)
                {
                    try
                    {
                        return (Bitmap)Image.FromStream(new MemoryStream(art));
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                return null;
            }
        }

        public TimeSpan Duration
        {
            get 
            {
                var seconds = (float)this.GetLongValue(Constants.g_wszWMDuration) / 10000000;
                return TimeSpan.FromSeconds(seconds);
            }
        }

        private long GetLongValue(string valueName)
        {
            var value = GetValue(valueName);
            if (value == null || value.Length == 0)
            {
                return 0L;
            }

            return BitConverter.ToInt64(value, 0);
        }

        private string GetStringValue(string valueName)
        {
            var value = GetValue(valueName);
            if (value == null || value.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.Unicode.GetString(value);
        }


        private byte[] GetValue(string valueName)
        {
            short stream = (short)0;
            AttrDataType type;
            short datalen = 0;
            byte[] obj;

            try
            {
                this.wmaHeader.GetAttributeByName(ref stream, valueName, out type, null, ref datalen);
                obj = new byte[datalen];

                this.wmaHeader.GetAttributeByName(ref stream, valueName, out type, (byte[])obj, ref datalen);

                return obj;
            }
            catch (Exception e)
            {
                Trace.WriteLine("Wma Metadata Exception: " + e);
            }

            return null;
        }
    }
}
