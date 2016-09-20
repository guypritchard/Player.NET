namespace DJPad.Db
{
    using System.Data.Entity;
    using System.Linq;

    [DbConfigurationType(typeof(SqlCeMediaCorpusConfiguration))]
    public partial class MediaCollectionContainer : DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Album>().HasMany(a => a.MediaItems);
            modelBuilder.Entity<MediaItem>().HasRequired(mi => mi.Album);
        }

        public DbSet<MediaItem> MediaItems { get; set; }
        public DbSet<Album> Albums { get; set; }
        private DbSet<MediaCorpusInfo> CorpusInfos { get; set; }

        public MediaCorpusInfo CorpusInfo
        {
            get { return CorpusInfos.FirstOrDefault(); }
        }
    }
}