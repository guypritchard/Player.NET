

namespace DJPadTests
{
    using DJPad.Player;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PlaylistItemTests
    {
        [TestMethod]
        public void SourceFileMissing()
        {
            var item = new PlaylistItem(@"c:\doesn'texist.mp3");
            Assert.IsFalse(item.IsPlayable());
        }
    }
}
