
namespace DJPad.Db
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class MediaCorpusInfo
    {
        [Key]
        public int Id { get; set; }

        public string CorpusScanLocation { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
