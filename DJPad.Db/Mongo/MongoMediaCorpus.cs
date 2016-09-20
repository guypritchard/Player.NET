namespace DJPad.Db.Mongo
{
    using System.Collections.Generic;
    using System.Linq;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using MongoDB.Driver;

    public class MongoMediaCorpus : IMediaCorpus
    {
        private IMongoDatabase database;

        public void Initialize(string sourceDirectory = null, string corpusLocation = null, bool forceRecreate = false)
        {
            if (this.database == null)
            {
                var server = new MongoClient();
                this.database = server.GetDatabase("MediaCorpus");
            }
        }

        public void AddOrUpdate(Core.Interfaces.IMetadata metadata, string location)
        {
            var albumCollection = this.database.GetCollection<Album>("Albums");

            var existingAlbum = albumCollection.FindSync(a => a.Name == metadata.Album).FirstOrDefault();
            if (existingAlbum == null)
            {
                existingAlbum = new Album()
                {
                    Id = StaticRandom.Next(int.MaxValue),
                    Name = metadata.Album,
                    ImageBytes = metadata.AlbumArt.ToByteArray()
                };

                albumCollection.InsertOne(existingAlbum);
            }

            var mediaCollection = this.database.GetCollection<MediaItem>("MediaItems");


            var existingFile = mediaCollection.FindSync(a => a.FileLocation == location).FirstOrDefault();
            if (existingFile == null)
            {
                existingFile = new MediaItem()
                {
                    Id = StaticRandom.Next(int.MaxValue),
                    Artist = metadata.Artist,
                    FileLocation = location,
                    Title = metadata.Title,
                };

                mediaCollection.InsertOneAsync(existingFile).Wait();

                if (existingAlbum.MediaItems.FirstOrDefault(m => m.FileLocation == location) == null)
                {
                    existingAlbum.MediaItems.Add(existingFile);
                }

                albumCollection.UpdateOne(f => f.Id == existingAlbum.Id, Builders<Album>.Update.Push("MediaItems", existingFile));
            }
        }

        public IEnumerable<IAlbum> Albums()
        {
            var albumCollection = this.database.GetCollection<Album>("Albums");
            return (IEnumerable<IAlbum>)albumCollection.AsQueryable();
        }

        public int AlbumCount()
        {
            return 0;
        }
    }
}
