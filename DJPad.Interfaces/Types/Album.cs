using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.Interfaces.Types
{
    using DJPad.Core.Interfaces;

    public class Album : IAlbum
    {
        public int Id { get; set; }
        public byte[] ImageBytes { get; set; }
        public string Name { get; set; }
        public IEnumerable<IMediaItem> MediaItems { get; set; }
    }
}
