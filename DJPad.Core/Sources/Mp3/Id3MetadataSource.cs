namespace DJPad.Sources
{
    using System.Collections.Generic;
    using System.Linq;

    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using ID3;
    using System.IO;
    using System;
    using System.Drawing;

    internal class Id3MetadataSource : IMetadata
    {
        #region Fields

        private readonly IList<Id3V2Frame> tags;

        private readonly string fileName;

        #endregion

        #region Constructors and Destructors

        public Id3MetadataSource(IList<Id3V2Frame> tags, string fileName)
        {
            this.tags = tags;
            this.fileName = fileName;
        }

        #endregion

        #region Public Properties

        public string Album
        {
            get
            {
                return this.getTagText(Id3FrameType.TALB);
            }
        }

        public string Artist
        {
            get
            {
                return this.getTagText(Id3FrameType.TPE1);
            }
        }

        public string Title
        {
            get
            {
                var title = this.getTagText(Id3FrameType.TIT2);;

                if (string.IsNullOrEmpty(title))
                {
                    title = Path.GetFileName(this.fileName);
                }

                return title;
            }
        }

        public Bitmap AlbumArt
        {
            get
            {
                var art = this.getTagImage(Id3FrameType.APIC) ?? AlternateArtSource.FileSourceArt(Path.GetDirectoryName(this.fileName));

                // If we didn't find a poster in Id3 - we can scan for Folder.jpg and AlbumArt_{Guid}_{Size}.jpg
                if (art != null && art.Length > 0)
                {
                    try
                    {
                        return (Bitmap) Image.FromStream(new MemoryStream(art));
                    }
                    catch (ArgumentException)
                    {
                        //try
                        //{
                        //    using (var ms = new MemoryStream(art))
                        //    using (var fs = File.OpenWrite("BadImage.dat"))
                        //    {
                        //        ms.CopyTo(fs);
                        //    }
                        //}
                        //catch
                        //{
                        //}
                    }
                }

                return null;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                var milliseconds = 0;
                if (int.TryParse(this.getTagText(Id3FrameType.TLEN), out milliseconds))
                {
                    return TimeSpan.FromMilliseconds(milliseconds);
                }

                return TimeSpan.Zero;
            }
        }

        #endregion

        #region Methods

        private string getTagText(Id3FrameType frame)
        {
            if (this.tags != null)
            {
                Id3V2Frame tag = this.tags.FirstOrDefault(f => f.Type == frame);
                if (tag != null && tag.IsValid())
                {
                    return ((TextFrame)tag).Text;
                }
            }
            
            return string.Empty;
        }

        private byte[] getTagImage(Id3FrameType frame)
        {
            if (this.tags != null)
            {
                Id3V2Frame tag = this.tags.FirstOrDefault(f => f.Type == frame);
                if (tag is ImageFrame && tag.IsValid())
                {
                    return ((ImageFrame) tag).ImageData;
                }
            }

            return null;
        }

        #endregion
    }
}