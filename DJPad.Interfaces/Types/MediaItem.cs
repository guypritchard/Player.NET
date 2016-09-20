namespace DJPad.Interfaces.Types
{
    using DJPad.Core.Interfaces;

    public class MediaItem : IMediaItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FileLocation { get; set; }
        public string Artist { get; set; }
        public IAlbum Album { get; set; }
    }
}
