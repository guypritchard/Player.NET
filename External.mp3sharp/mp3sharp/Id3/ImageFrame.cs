namespace ID3
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class ImageFrame : Id3V2Frame
    {
        #region Fields

        private readonly bool isValid;

        #endregion

        #region Constructors and Destructors

        public ImageFrame(Id3V2Frame frame)
            : base(frame)
        {
            this.isValid = this.Parse();
        }

        #endregion

        #region Public Properties

        public string Description { get; private set; }

        public byte[] ImageData { get; private set; }

        public string MimeType { get; private set; }

        public short PictureType { get; private set; }

        public short TextEncodingType { get; private set; }

        #endregion

        #region Public Methods and Operators

        public override bool IsValid()
        {
            return this.isValid;
        }

        public bool Parse()
        {
            if (this.data == null)
            {
                throw new Exception("No data.");
            }

            int currentPosition = 0;
            this.TextEncodingType = this.data[currentPosition++];

            string mimeType;
            if ((currentPosition = this.TryReadString(this.TextEncodingType, currentPosition, out mimeType)) == -1)
            {
                Debug.WriteLine("Mime type not found!");
                return false;
            }
            this.MimeType = mimeType;

            this.PictureType = this.data[currentPosition++];

            string description;
            if ((currentPosition = this.TryReadString(this.TextEncodingType, currentPosition, out description)) == -1)
            {
                Debug.WriteLine("Description not found!");
                return false;
            }
            this.Description = description;

            this.ImageData = new byte[this.data.Length - currentPosition];
            Array.Copy(this.data, currentPosition, this.ImageData, 0, this.ImageData.Length);

            return true;
        }

        public void WriteImage(string filename)
        {
            if (!this.isValid || this.ImageData == null)
            {
                throw new Exception("Invalid image cannot output.");
            }

            using (FileStream fs = File.Create(filename))
            {
                fs.Write(this.ImageData, 0, this.ImageData.Length);
            }
        }

        #endregion
    }
}