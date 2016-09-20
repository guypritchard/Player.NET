namespace DJPad.Core.Utils
{
    using System.Linq;
    using System.IO;

    public static class AlternateArtSource
    {
        /// <summary>
        /// Find album art in the same directory as the specified playable media file.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static byte[] FileSourceArt(string directory)
        {
            byte[] albumArtData = null;
            
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.jpg");
                var foundArt = files.Where(f =>
                {
                    var file = Path.GetFileName(f);
                    return file.StartsWith("Folder") || file.StartsWith("AlbumArt");
                }).OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();

                if (!string.IsNullOrEmpty(foundArt))
                {
                    albumArtData = File.ReadAllBytes(foundArt);
                }
            }

            return albumArtData;
        }
    }
}
