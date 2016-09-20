namespace ID3
{
    using System;
    using System.Diagnostics;

    public class TextFrame : Id3V2Frame
    {
        #region Fields

        private bool isValid;

        #endregion

        #region Constructors and Destructors

        public TextFrame(Id3V2Frame frame)
            : base(frame)
        {
            this.isValid = this.Parse();
        }

        #endregion

        public override bool IsValid()
        {
            return base.IsValid() && this.isValid;
        }

        #region Public Properties

        public string Text { get; private set; }

        public short TextEncodingType { get; private set; }

        #endregion

        #region Public Methods and Operators

        public bool Parse()
        {
            if (this.data == null)
            {
                throw new Exception("No data.");
            }

            int currentPosition = 0;
            this.TextEncodingType = this.data[currentPosition++];

            string text;
            if (this.TryReadString(this.TextEncodingType, currentPosition, out text) == -1)
            {
                Debug.WriteLine("Mime type not found!");
                return false;
            }
            this.Text = text.Trim();

            return true;
        }

        #endregion
    }
}