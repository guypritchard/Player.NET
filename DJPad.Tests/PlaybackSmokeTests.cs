namespace DJPadTests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using DJPad.Core;
    using DJPad.Player;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PlaybackSmokeTests
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void PlaybackLifecycle()
        {
            var file = Environment.GetEnvironmentVariable("DJPAD_SMOKE_FILE");
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Assert.Inconclusive("Set DJPAD_SMOKE_FILE to a playable local audio file.");
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var directory = Path.Combine(Path.GetTempPath(), $"DJPad-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var extension = Path.GetExtension(file);
            var firstFile = Path.Combine(directory, $"01{extension}");
            var selectedFile = Path.Combine(directory, $"02{extension}");
            var lastFile = Path.Combine(directory, $"03{extension}");
            File.Copy(file, firstFile);
            File.Copy(file, selectedFile);
            File.Copy(file, lastFile);
            var player = new PlayerState();

            try
            {
                player.NewPlaylist(selectedFile, true);
                Thread.Sleep(1500);

                Assert.AreEqual(3, player.Playlist.Count, "Opening one file did not build a playlist from its directory.");
                Assert.AreEqual(selectedFile, player.Playlist.Current.FullFileName, true,
                    "Directory playback did not start at the selected file's relative position.");
                Assert.IsTrue(player.IsPlaying, "Playback stopped during the initial buffer.");
                Assert.IsTrue(player.Playlist.Current.Source.Position > TimeSpan.Zero, "Playback position did not advance.");

                var seekPosition = TimeSpan.FromSeconds(Math.Min(30, player.Playlist.Current.Source.Duration.TotalSeconds / 2));
                player.Playlist.Current.Source.Position = seekPosition;
                Thread.Sleep(500);
                Assert.IsTrue(player.IsPlaying, "Playback paused after seeking.");
                Assert.IsTrue(player.Playlist.Current.Source.Position >= seekPosition, "Playback did not continue from the seek position.");

                SelectAndPlay(player, player.Playlist.Items[0]);
                Thread.Sleep(750);
                Assert.AreSame(player.Playlist.Items[0], player.Playlist.Current, "The first playlist item could not be selected.");
                Assert.IsTrue(player.Playlist.Start, "The playlist did not report its first-item boundary.");
                Assert.IsTrue(player.IsPlaying, "Selecting the first item did not begin playback.");

                var outgoing = player.Playlist.Current;
                player.Next();
                Assert.AreEqual(PlaylistItemState.Loaded, outgoing.State,
                    "The outgoing decoder was disposed before the crossfade completed.");
                Thread.Sleep(750);
                Assert.AreSame(player.Playlist.Items[1], player.Playlist.Current, "Next did not select the second item.");
                Assert.AreEqual(PlaylistItemState.Unloaded, outgoing.State,
                    "The outgoing decoder was not released after the crossfade.");

                player.Previous();
                Thread.Sleep(750);
                Assert.AreSame(player.Playlist.Items[0], player.Playlist.Current, "Previous did not return to the first item.");
                Assert.IsTrue(player.IsPlaying, "Playback did not continue after Previous.");

                SelectAndPlay(player, player.Playlist.Items[2]);
                SelectAndPlay(player, player.Playlist.Items[0]);
                Thread.Sleep(1000);
                Assert.AreSame(player.Playlist.Items[0], player.Playlist.Current,
                    "A stale output completion advanced the rapidly selected track.");
                Assert.IsTrue(player.IsPlaying, "Rapid track selection stopped playback.");

                player.Stop();
                Assert.IsFalse(player.IsPlaying, "Stop did not stop playback.");

                player.TogglePlay();
                Thread.Sleep(1000);
                Assert.IsTrue(player.IsPlaying, "Playback did not restart after stopping.");
            }
            finally
            {
                player.Stop();
                if (player.Audio is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                foreach (var item in player.Playlist.Items)
                {
                    item.Dispose();
                }
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void PlaylistFirstItemBoundary()
        {
            var items = new[]
            {
                (IPlaylistItem)new PlaylistItem(@"C:\one.mp3"),
                new PlaylistItem(@"C:\two.mp3"),
                new PlaylistItem(@"C:\three.mp3")
            };
            var playlist = new Playlist(items);

            playlist.Current = items[1];
            playlist.MovePrevious();

            Assert.AreSame(items[0], playlist.Current);
            Assert.IsTrue(playlist.Start);
            Assert.IsNull(playlist.Previous);
            Assert.AreSame(items[1], playlist.Next);
        }

        [TestMethod]
        public void PlaybackOrderMatchesRandomPlaylistOrder()
        {
            var items = new[]
            {
                (IPlaylistItem)new PlaylistItem(@"C:\one.mp3"),
                new PlaylistItem(@"C:\two.mp3"),
                new PlaylistItem(@"C:\three.mp3")
            };
            var playlist = new Playlist(items) { Current = items[1], Random = true };
            var currentIndex = playlist.PlaybackItems.IndexOf(playlist.Current);

            Assert.IsTrue(currentIndex >= 0);
            if (currentIndex < playlist.PlaybackItems.Count - 1)
            {
                var expected = playlist.PlaybackItems[currentIndex + 1];
                playlist.MoveNext();
                Assert.AreSame(expected, playlist.Current);
            }
        }

        private static void SelectAndPlay(PlayerState player, IPlaylistItem item)
        {
            player.Playlist.Current = item;
            player.Play(item, true);
        }
    }
}
