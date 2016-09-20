namespace DJPad.Player
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DJPad.Core;

    public class PlaylistGenerator
    {
        #region Public Methods and Operators

        public static Playlist FromDirectory(string path, bool recursive = false)
        {
            var files = Directory.GetFiles(path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(SourceRegistry.IsSupported)
                .Select((file, index) => (IPlaylistItem)new PlaylistItem(file, index))
                .ToList();

            return new Playlist(files);
        }

        public static Playlist FromPlaylistFile(string playlistFile)
        {
            var files = File.ReadAllLines(playlistFile)
                .Where(file => File.Exists(file) && SourceRegistry.IsSupported(file))
                .Select((file, index) => (IPlaylistItem)new PlaylistItem(file, index))
                .ToList();

            return new Playlist(files) { FileName = playlistFile };
        }

        public static Playlist FromMultiplePaths(string[] paths)
        {
            var files = new List<string>();

            foreach(var path in paths)
            {
                if (Directory.Exists(path))
                {
                    files.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                }
                else if (File.Exists(path))
                {
                    files.Add(path);
                }
            }

            var playListItems = files.Where(file => SourceRegistry.IsSupported(file))
                .Select((file, index) => (IPlaylistItem)new PlaylistItem(file, index))
                .ToList();

            return new Playlist(playListItems);
        }

        #endregion
    }
}