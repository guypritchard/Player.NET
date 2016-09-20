using System;
using System.Collections.Generic;
using System.Linq;


namespace DJPad.Core.Player.Playlist
{
    using DJPad.Core.Interfaces;
    using System.Collections.Concurrent;

    public interface IIndexable
    {
        string[] GetStrings();
    }

    public class Corpus <T, U> 
        where T : IIndexable
        where U : IMetadata
    {
        private readonly ConcurrentDictionary<string, List<T>> index = new ConcurrentDictionary<string, List<T>>();

        public void Add(T item, U metadata)
        {
            foreach(var searchString in this.GetStrings(metadata))
            {
                var storableString = searchString.ToUpperInvariant();

                if (index.ContainsKey(storableString))
                {
                    index[storableString].Add(item);
                }
                else
                {
                    index[storableString] = new List<T>();
                }
            }
        }
        
        public string[] GetStrings(IMetadata metadata)
        {
            var list = new List<string>();
            list.AddRange(metadata.Album.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            list.AddRange(metadata.Artist.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
            list.AddRange(metadata.Title.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
        
            return list.ToArray();
        }


        public IEnumerable<T> Find(string text)
        {
            if (this.index.ContainsKey(text.ToUpper()))
            {
                return this.index[text];
            }

            return Enumerable.Empty<T>();
        }
    }
}
