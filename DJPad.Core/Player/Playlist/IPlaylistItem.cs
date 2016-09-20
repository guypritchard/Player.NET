
namespace DJPad.Core
{
    using DJPad.Core.Interfaces;
    using DJPad.Core.Player.Playlist;
    using System;

    public enum DataType { Directory, Media }

    public interface INagivationItem : IPlaylistItem
    {

    }

    public interface IPlaylistItem : IIndexable, IDisposable
    {
        string FileName { get; }
        string FullFileName { get; }
        bool HasLoadedMetadata { get; }
        int Index { get; set; }
        DJPad.Core.Interfaces.IMetadata Metadata { get; }
        int RandomIndex { get; }
        DJPad.Core.Interfaces.IFileSource Source { get; }
        DJPad.Player.PlaylistItemState State { get; }
        System.Drawing.Bitmap SynchronousArt { get; }

        event EventHandler<IMetadata> MetadataLoaded;
    }
}
