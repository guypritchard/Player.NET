
namespace DJPad.Db
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlServerCe;
    using System.IO;
    using System.Security;
    using System.Linq;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;

    public class SqlCeMediaCorpus : IMediaCorpus
    {
        private const string ApplicationName = "Player.Net.2";
        private const string mediaDbName = "MediaDb.sdf";
        private const string connectionFormatString = "DataSource=\"{0}\"";
        private const string entityFrameworkString = @"metadata=res://*/MediaCollection.csdl|res://*/MediaCollection.ssdl|res://*/MediaCollection.msl;provider=System.Data.SqlServerCe.4.0;provider connection string='{0}';";

        private SecureString password = new SecureString();

        public static string DbLocation { get; private set; }

        public static string DefaultLocation
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ApplicationName);
            }
        }

        public SqlCeMediaCorpus(string location, SecureString password = null)
        {
            SqlCeMediaCorpus.DbLocation = Path.Combine(location, mediaDbName);
            this.password = this.password ?? password;
        }

        public SqlCeMediaCorpus() : this(DefaultLocation)
        {
        }

        public void Initialize(string sourceDirectory = null, string corpusLocation = null, bool forceRecreate = false)
        {
            if (corpusLocation == null)
            {
                corpusLocation = DbLocation;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(corpusLocation));
            var exists = File.Exists(corpusLocation);

            if (forceRecreate && exists)
            {
                File.Delete(corpusLocation);
            }

            SqlCeEngine engine = new SqlCeEngine(
                string.Format(
                    connectionFormatString,
                    corpusLocation,
                    password.ToString() ?? string.Empty));

            if (!exists)
            {
                engine.CreateDatabase();
            }
        }

        public void AddOrUpdate(IMetadata metadata, string location)
        {
            using (var c = new MediaCollectionContainer())
            {
                var existingAlbum = c.Albums.Where(a => a.Name == metadata.Album).FirstOrDefault();
                if (existingAlbum == null)
                {
                    var newAlbum = new Album()
                                 {
                                     Name = metadata.Album,
                                     ImageBytes = metadata.AlbumArt.ToByteArray()
                                 };

                    c.Albums.Add(newAlbum);

                    c.SaveChanges();
                    existingAlbum = c.Albums.Where(a => a.Name == metadata.Album).FirstOrDefault();
                }

                var newMediaItem = new MediaItem()
                                 {
                                     Album = existingAlbum,
                                     Artist = metadata.Artist,
                                     FileLocation = location,
                                     Title = metadata.Title,
                                 };

                c.MediaItems.Add(newMediaItem);
                existingAlbum.MediaItems.Add(newMediaItem);

                c.SaveChanges();
            }
        }

        public IEnumerable<IAlbum> Albums()
        {
            var c = new MediaCollectionContainer();
            foreach (var album in c.Albums.Include("MediaItems"))
            {
                yield return StorageMapper.ToAbstract(album); ;
            }
        }

        public int AlbumCount()
        {
            var c = new MediaCollectionContainer();
            return c.Albums.Count();
        }
    }
}
