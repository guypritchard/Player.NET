using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.Db
{
    using DJPad.Core.Interfaces;

    public static class StorageMapper
    {
        public static IAlbum ToAbstract(Album album)
        {
            return new Interfaces.Types.Album
            {
                Id = album.Id,
                ImageBytes = album.ImageBytes,
                Name = album.Name,
                MediaItems = album.MediaItems.Select(ToAbstract)
            };
        }

        public static IMediaItem ToAbstract(MediaItem item)
        {
            return new Interfaces.Types.MediaItem
            {
                Id = item.Id,
                Title = item.Title,
                Album = ToAbstract(item.Album),
                Artist = item.Artist,
                FileLocation = item.FileLocation,
            };
        }
    }
}
