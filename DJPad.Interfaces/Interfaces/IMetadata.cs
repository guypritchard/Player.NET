namespace DJPad.Core.Interfaces
{
    using System;
    using System.Drawing;

    public interface IMetadata
    {
        #region Public Properties

        string Album { get; }

        string Artist { get; }

        string Title { get; }

        Bitmap AlbumArt { get; }

        TimeSpan Duration { get; }

        #endregion
    }
}