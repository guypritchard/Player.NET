using DJPad.Core;
using DJPad.Player;

namespace DJPad.Experimental;

public sealed class PlayerController : IDisposable
{
    private readonly PlayerState player = new();
    private readonly HashSet<IPlaylistItem> metadataSubscriptions = new();

    public PlayerController()
    {
        player.PlayStateChanged += (_, _) => Changed?.Invoke();
        player.ItemLoadStateChanged += _ => Changed?.Invoke();
        player.PlaylistChanged += () =>
        {
            SubscribeToMetadata();
            Changed?.Invoke();
        };
        player.Init();
        SubscribeToMetadata();
    }

    public event Action? Changed;

    public PlayerState State => player;

    public IReadOnlyList<IPlaylistItem> Tracks => player.Playlist.PlaybackItems;

    public int CurrentIndex => player.Playlist.PlaybackItems.IndexOf(player.Playlist.Current);

    public IPlaylistItem? Current => player.Playlist.Current;

    public bool IsPlaying => player.IsPlaying;

    public void Open(IEnumerable<string> paths)
    {
        var selected = paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (selected.Length == 0)
        {
            return;
        }

        if (selected.Length == 1)
        {
            player.NewPlaylist(selected[0], true);
        }
        else
        {
            player.NewPlaylist(selected, true);
        }
        Changed?.Invoke();
    }

    public void Play(IPlaylistItem item)
    {
        player.Playlist.Current = item;
        player.Play(item, true);
        Changed?.Invoke();
    }

    public void TogglePlay() => player.TogglePlay();

    public void Previous() => player.Previous();

    public void Next() => player.Next();

    public void Seek(double percentage)
    {
        if (Current?.Source == null)
        {
            return;
        }

        Current.Source.Position = TimeSpan.FromMilliseconds(Current.Source.Duration.TotalMilliseconds * Math.Clamp(percentage, 0, 1));
        Changed?.Invoke();
    }

    public void ToggleRandom()
    {
        player.Playlist.Random = !player.Playlist.Random;
        player.Configuration.Randomise = player.Playlist.Random;
        Changed?.Invoke();
    }

    public void ToggleRepeat()
    {
        player.Playlist.Repeat = !player.Playlist.Repeat;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        player.Stop();
        if (player.Audio is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void SubscribeToMetadata()
    {
        foreach (var track in player.Playlist.Items)
        {
            if (metadataSubscriptions.Add(track))
            {
                track.MetadataLoaded += (_, _) => Changed?.Invoke();
            }
        }
    }
}
