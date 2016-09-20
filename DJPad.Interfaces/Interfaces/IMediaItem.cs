namespace DJPad.Core.Interfaces
{
    public interface IMediaItem
    {
        int Id { get; set; }
        string Title { get; set; }
        string FileLocation { get; set; }
        string Artist { get; set; }
        IAlbum Album { get; set; }
    }
}