namespace DJPad.Core.Interfaces
{
    using System.Collections.Generic;
    using DJPad.Core.Interfaces;

    public interface IMediaCorpus
    {
        void Initialize(string sourceDirectory = null, string corpusLocation = null, bool forceRecreate = false);
        void AddOrUpdate(IMetadata metadata, string location);
        IEnumerable<IAlbum> Albums();

        int AlbumCount();
    }
}
