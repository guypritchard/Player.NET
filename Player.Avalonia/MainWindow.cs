using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DJPad.Core;

namespace DJPad.Experimental;

public sealed class MainWindow : Window
{
    private const int MainMode = 0;
    private const int MiniMode = 1;
    private const int MinimalMode = 2;
    private readonly PlayerController player = new();
    private readonly WindowsSpecificShell shell = new();
    private readonly DispatcherTimer taskbarTimer;
    private PlaylistWindow? playlistWindow;
    private ClassicPlayerView? classicView;
    private IPlaylistItem? taskbarItem;
    private bool taskbarMetadataLoaded;
    private bool taskbarPlaying;
    private int taskbarProgress = -1;
    private bool? taskbarProgressPlaying;

    public MainWindow()
    {
        Title = "Player.NET";
        using (var iconStream = AssetLoader.Open(new Uri("avares://Player.NET/Assets/Player.ico")))
        {
            Icon = new WindowIcon(iconStream);
        }
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        Background = Avalonia.Media.Brushes.Black;

        var savedMode = player.State.Configuration.Mode;
        SetPlayerMode(savedMode is MainMode or MiniMode or MinimalMode ? savedMode : MainMode, false);
        taskbarTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background,
            (_, _) => UpdateTaskbarProgress());

        Opened += (_, _) =>
        {
            UpdateTaskbarOverlay();
            UpdateTaskbarProgress();
            taskbarTimer.Start();
        };
        player.Changed += OnPlayerChanged;
        Closed += (_, _) =>
        {
            player.Changed -= OnPlayerChanged;
            taskbarTimer.Stop();
            player.Dispose();
        };
        PositionChanged += (_, _) => PositionPlaylist();
    }

    private void OnPlayerChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateTaskbarOverlay();
            UpdateTaskbarProgress();
        });
    }

    private void UpdateTaskbarOverlay()
    {
        var item = player.Current;
        if (item == null)
        {
            return;
        }

        var playing = player.IsPlaying;
        if (ReferenceEquals(item, taskbarItem) && playing == taskbarPlaying
            && item.HasLoadedMetadata == taskbarMetadataLoaded)
        {
            return;
        }

        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        shell.SetOverlayIcon(handle, item, playing);
        taskbarItem = item;
        taskbarPlaying = playing;
        taskbarMetadataLoaded = item.HasLoadedMetadata;
    }

    private void UpdateTaskbarProgress()
    {
        var source = player.Current?.Source;
        if (source == null || source.Duration <= TimeSpan.Zero)
        {
            return;
        }

        var progress = (int)Math.Clamp(source.Position.TotalMilliseconds / source.Duration.TotalMilliseconds * 1000, 0, 1000);
        var playing = player.IsPlaying;
        if (progress == taskbarProgress && playing == taskbarProgressPlaying)
        {
            return;
        }

        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        shell.SetPlaybackProgress(handle, progress, playing);
        taskbarProgress = progress;
        taskbarProgressPlaying = playing;
    }

    private void SetPlayerMode(int mode, bool save = true)
    {
        const double width = 400;
        var mini = mode == MiniMode;
        var minimal = mode == MinimalMode;
        var height = mini ? 50 : 400;
        MinWidth = width;
        MaxWidth = width;
        Width = width;
        MinHeight = height;
        MaxHeight = height;
        Height = height;
        Topmost = mini;

        if (mini)
        {
            playlistWindow?.Hide();
            classicView = null;
            var miniView = new MiniPlayerView(player);
            miniView.ExpandRequested += () => SetPlayerMode(MainMode);
            miniView.OpenRequested += async () => await OpenFilesAsync();
            miniView.CloseRequested += Close;
            miniView.DragRequested += BeginMoveDrag;
            Content = miniView;
        }
        else
        {
            classicView = new ClassicPlayerView(player, minimal)
            {
                PlaylistVisible = playlistWindow?.IsVisible == true
            };
            classicView.NextModeRequested += () => SetPlayerMode(minimal ? MiniMode : MinimalMode);
            classicView.OpenRequested += async () => await OpenFilesAsync();
            classicView.CloseRequested += Close;
            classicView.PlaylistRequested += TogglePlaylist;
            classicView.DragRequested += BeginMoveDrag;
            Content = classicView;
        }

        if (save)
        {
            player.State.Configuration.Mode = mode;
            player.State.Configuration.Save();
        }
    }

    private void TogglePlaylist()
    {
        playlistWindow ??= new PlaylistWindow(player);
        if (playlistWindow.IsVisible)
        {
            playlistWindow.Hide();
            if (classicView != null) classicView.PlaylistVisible = false;
            return;
        }

        PositionPlaylist();
        playlistWindow.Show(this);
        PositionPlaylist();
        if (classicView != null) classicView.PlaylistVisible = true;
    }

    private void PositionPlaylist()
    {
        if (playlistWindow != null)
        {
            var widthInPixels = (int)Math.Ceiling(ClientSize.Width * RenderScaling);
            playlistWindow.Position = new PixelPoint(Position.X + widthInPixels, Position.Y);
        }
    }

    private async Task OpenFilesAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open playable files",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Playable files") { Patterns = new[] { "*.mp3", "*.wav", "*.wma", "*.cda", "*.ppl" } }
            }
        });
        player.Open(files.Select(file => file.TryGetLocalPath()).OfType<string>());
    }
}
