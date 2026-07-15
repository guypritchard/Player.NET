using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DJPad.Core.Interfaces;
using DJPad.Core.Utils;
using DJPad.Core.Vis;
using DJPad.Types;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace DJPad.Experimental;

public sealed class ClassicPlayerView : Control
{
    private static readonly ColorPalette UnknownPalette = global::Resources.Resources.Unknown.GetPalette();
    private static readonly ColorPalette SpectrogramPalette = new(new[]
    {
        DrawingColor.FromArgb(255, 0, 220, 255),
        DrawingColor.FromArgb(255, 245, 55, 175),
        DrawingColor.FromArgb(255, 255, 245, 190)
    });

    private static readonly Rect PreviousBounds = new(50, 150, 100, 100);
    private static readonly Rect PlayBounds = new(133, 133, 134, 134);
    private static readonly Rect NextBounds = new(250, 150, 100, 100);
    private static readonly Rect OpenBounds = new(188, 133, 25, 25);
    private static readonly Rect VisualisationBounds = new(188, 240, 25, 25);
    private static readonly Rect RandomBounds = new(233, 213, 20, 20);
    private static readonly Rect RepeatBounds = new(233, 167, 20, 20);
    private static readonly Rect PlaylistBounds = new(367, 190, 20, 20);
    private static readonly Rect ProgressBounds = new(0, 330, 400, 20);
    private static readonly Rect CloseBounds = new(382, 2, 16, 16);

    private readonly PlayerController player;
    private readonly bool minimal;
    private readonly DispatcherTimer timer;
    private readonly BitmapBuffer visualisationBuffer = new();
    private readonly IVisualisation?[] visualisations =
    {
        new Oscilloscope(),
        new FftBars(),
        new Sonagram(),
        new BassZoom(),
        new CircularOscilloscope(),
        null,
        new PixelMosaic(),
        new LiquidArtwork()
    };
    private readonly Dictionary<string, Bitmap> assets = new(StringComparer.OrdinalIgnoreCase);
    private DrawingBitmap? albumArtSource;
    private Bitmap? albumArtImage;
    private ColorPalette? albumArtPalette;
    private DateTime lastPointerMove = DateTime.UtcNow;
    private bool controlsVisible;
    private bool seeking;
    private int visualisationIndex;
    private bool playlistVisible;

    public ClassicPlayerView(PlayerController player, bool minimal = false)
    {
        this.player = player;
        this.minimal = minimal;
        controlsVisible = !minimal;
        visualisationIndex = player.State.Configuration.Visualisation;
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.Arrow);
        player.Changed += OnPlayerChanged;
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, OnFrame);
        timer.Start();
        DetachedFromVisualTree += (_, _) =>
        {
            timer.Stop();
            player.Changed -= OnPlayerChanged;
            visualisationBuffer.Dispose();
            albumArtImage?.Dispose();
            foreach (var asset in assets.Values) asset.Dispose();
        };
    }

    public event Action? OpenRequested;
    public event Action? CloseRequested;
    public event Action? PlaylistRequested;
    public event Action? NextModeRequested;
    public event Action<PointerPressedEventArgs>? DragRequested;

    public bool PlaylistVisible
    {
        get => playlistVisible;
        set
        {
            playlistVisible = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scaleX = Bounds.Width / 400.0;
        var scaleY = Bounds.Height / 400.0;
        using (context.PushTransform(Matrix.CreateScale(scaleX, scaleY)))
        {
            DrawAlbumArt(context);
            DrawVisualisation(context);
            if (!minimal || controlsVisible)
            {
                if (!minimal)
                {
                    DrawPosition(context);
                }
                if (controlsVisible)
                {
                    DrawControls(context);
                }
                DrawProgress(context);
                DrawCurrentItem(context);
                if (controlsVisible)
                {
                    DrawAsset(context, "Cancel.png", CloseBounds);
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        lastPointerMove = DateTime.UtcNow;
        controlsVisible = true;
        if (seeking)
        {
            SeekTo(e.GetPosition(this));
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        if (minimal)
        {
            controlsVisible = true;
            InvalidateVisual();
        }
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        if (minimal && !seeking)
        {
            controlsVisible = false;
            InvalidateVisual();
        }
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = ToDesignPoint(e.GetPosition(this));
        if (e.ClickCount == 2 && !IsActionPoint(point))
        {
            NextModeRequested?.Invoke();
            return;
        }

        if (CloseBounds.Contains(point)) CloseRequested?.Invoke();
        else if (OpenBounds.Contains(point)) OpenRequested?.Invoke();
        else if (VisualisationBounds.Contains(point)) CycleVisualisation();
        else if (RandomBounds.Contains(point)) player.ToggleRandom();
        else if (RepeatBounds.Contains(point)) player.ToggleRepeat();
        else if (PlaylistBounds.Contains(point)) PlaylistRequested?.Invoke();
        else if (ProgressBounds.Contains(point))
        {
            seeking = true;
            e.Pointer.Capture(this);
            SeekTo(e.GetPosition(this));
        }
        else if (PreviousBounds.Contains(point)) player.Previous();
        else if (NextBounds.Contains(point)) player.Next();
        else if (PlayBounds.Contains(point)) player.TogglePlay();
        else DragRequested?.Invoke(e);

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (seeking)
        {
            SeekTo(e.GetPosition(this));
            seeking = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            if (minimal && !IsPointerOver)
            {
                controlsVisible = false;
            }
        }

        base.OnPointerReleased(e);
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (!minimal && controlsVisible && DateTime.UtcNow - lastPointerMove > TimeSpan.FromSeconds(1))
        {
            controlsVisible = false;
        }
        InvalidateVisual();
    }

    private void OnPlayerChanged()
    {
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    private void DrawAlbumArt(DrawingContext context)
    {
        var image = GetAlbumArtImage();
        if (image != null)
        {
            context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), new Rect(0, 0, 400, 400));
            return;
        }
        DrawAsset(context, "unknown.jpg", new Rect(0, 0, 400, 400));
    }

    private void DrawVisualisation(DrawingContext context)
    {
        var visualisation = visualisations[visualisationIndex % visualisations.Length];
        if (visualisation == null || !player.State.CanPlay)
        {
            return;
        }

        visualisation.SampleSource = player.State.Audio;
        if (visualisation is IArtworkVisualisation artworkVisualisation)
        {
            artworkVisualisation.Artwork = player.Current?.SynchronousArt ?? global::Resources.Resources.Unknown;
        }
        var palette = visualisation is Sonagram
            ? SpectrogramPalette
            : GetPalette();
        var frame = visualisation.Draw(new System.Drawing.Size(400, 400), DrawingColor.Transparent,
            player.IsPlaying, player.Current?.Source.Duration, palette);
        if (frame == null)
        {
            return;
        }
        if (visualisation is Sonagram)
        {
            context.FillRectangle(Brushes.Black, new Rect(0, 0, 400, 400));
        }
        var image = visualisationBuffer.Update(frame);
        context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), new Rect(0, 0, 400, 400));
    }

    private void DrawPosition(DrawingContext context)
    {
        if (player.Current?.Source == null)
        {
            return;
        }

        var palette = GetPalette();
        var text = player.Current.Source.Position.ToString(@"m\:ss");
        var formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Lucida Console"), 66, ToAvalonia(palette.Brightest, 0.5));
        var outline = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Lucida Console"), 66, Brushes.SlateGray);
        DrawOutlinedText(context, formatted, outline, new Point(200 - formatted.Width / 2, 200 - formatted.Height / 2));
    }

    private void DrawControls(DrawingContext context)
    {
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(150, 50, 50, 50)), new Rect(0, 0, 400, 400));
        DrawAsset(context, "Folder_White.png", OpenBounds);
        DrawAsset(context, player.State.Playlist.Repeat ? "Repeat.png" : "RepeatDisabled.png", RepeatBounds);
        DrawAsset(context, player.State.Playlist.Random ? "Random.png" : "RandomDisabled.png", RandomBounds);
        DrawAsset(context, "settings.png", VisualisationBounds);
        DrawAsset(context, PlaylistVisible ? "playlist.png" : "playlist1.png", PlaylistBounds);

        if (player.State.CanPlay)
        {
            DrawAsset(context, player.IsPlaying ? "player pause.png" : "player play.png", PlayBounds);
            if (!player.State.Playlist.Start) DrawAsset(context, "Player Previous.png", PreviousBounds);
            if (!player.State.Playlist.End) DrawAsset(context, "Player Next.png", NextBounds);
        }
    }

    private void DrawProgress(DrawingContext context)
    {
        if (player.Current?.Source == null)
        {
            return;
        }

        var duration = player.Current.Source.Duration.TotalSeconds;
        var progress = duration <= 0 ? 0 : player.Current.Source.Position.TotalSeconds / duration;
        context.DrawLine(new Pen(Brushes.White, 1), new Point(0, 349), new Point(400, 349));
        var x = Math.Clamp(progress, 0, 1) * 400;
        context.DrawLine(new Pen(Brushes.White, 5), new Point(x, 330), new Point(x, 350));
    }

    private void DrawCurrentItem(DrawingContext context)
    {
        var item = player.Current;
        if (item == null)
        {
            return;
        }

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(20, 50, 50, 50)), new Rect(0, 350, 400, 50));
        var albumArt = GetAlbumArtImage();
        if (albumArt != null)
        {
            context.DrawImage(albumArt, new Rect(0, 0, albumArt.Size.Width, albumArt.Size.Height), new Rect(0, 350, 50, 50));
        }
        else
        {
            DrawAsset(context, "unknown.jpg", new Rect(0, 350, 50, 50));
        }
        var title = new FormattedText(item.Metadata.Title ?? item.FileName, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold), 13, Brushes.White);
        var titleOutline = new FormattedText(item.Metadata.Title ?? item.FileName, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold), 13, Brushes.SlateGray);
        DrawOutlinedText(context, title, titleOutline, new Point(56, 351));
        var details = new FormattedText($"{item.Metadata.Album}\n{item.Metadata.Artist}", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Italic), 11, Brushes.Azure);
        var detailsOutline = new FormattedText($"{item.Metadata.Album}\n{item.Metadata.Artist}", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Italic), 11, Brushes.SlateGray);
        DrawOutlinedText(context, details, detailsOutline, new Point(56, 368));
        var duration = new FormattedText(item.Metadata.Duration.ToString(@"m\:ss"), System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), 13, Brushes.White);
        var durationOutline = new FormattedText(item.Metadata.Duration.ToString(@"m\:ss"), System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), 13, Brushes.SlateGray);
        DrawOutlinedText(context, duration, durationOutline, new Point(394 - duration.Width, 351));
    }

    private void DrawAsset(DrawingContext context, string name, Rect destination)
    {
        if (!assets.TryGetValue(name, out var image))
        {
            using var stream = AssetLoader.Open(new Uri($"avares://Player.NET/Assets/{Uri.EscapeDataString(name)}"));
            image = new Bitmap(stream);
            assets[name] = image;
        }
        context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), destination);
    }

    private ColorPalette GetPalette()
    {
        var source = player.Current?.SynchronousArt;
        if (source == null)
        {
            return UnknownPalette;
        }
        if (!ReferenceEquals(source, albumArtSource) || albumArtPalette == null)
        {
            UpdateAlbumArt(source);
        }
        return albumArtPalette ?? UnknownPalette;
    }

    private Bitmap? GetAlbumArtImage()
    {
        var source = player.Current?.SynchronousArt;
        if (source == null)
        {
            return null;
        }
        if (!ReferenceEquals(source, albumArtSource) || albumArtImage == null)
        {
            UpdateAlbumArt(source);
        }
        return albumArtImage;
    }

    private void UpdateAlbumArt(DrawingBitmap source)
    {
        albumArtImage?.Dispose();
        albumArtSource = source;
        albumArtImage = ConvertBitmap(source);
        albumArtPalette = source.GetPalette();
    }

    private static Bitmap ConvertBitmap(DrawingBitmap source)
    {
        using var stream = new MemoryStream();
        source.Save(stream, DrawingImageFormat.Png);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    private static IBrush ToAvalonia(DrawingColor color, double opacity)
    {
        return new SolidColorBrush(Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B));
    }

    private static void DrawOutlinedText(DrawingContext context, FormattedText text, FormattedText outline, Point position)
    {
        context.DrawText(outline, position + new Vector(-1, 0));
        context.DrawText(outline, position + new Vector(1, 0));
        context.DrawText(outline, position + new Vector(0, -1));
        context.DrawText(outline, position + new Vector(0, 1));
        context.DrawText(text, position);
    }

    private Point ToDesignPoint(Point point) => new(point.X * 400 / Bounds.Width, point.Y * 400 / Bounds.Height);

    private static bool IsActionPoint(Point point) =>
        CloseBounds.Contains(point) || OpenBounds.Contains(point) || PlayBounds.Contains(point) ||
        PreviousBounds.Contains(point) || NextBounds.Contains(point) || VisualisationBounds.Contains(point) ||
        RandomBounds.Contains(point) || RepeatBounds.Contains(point) || PlaylistBounds.Contains(point) || ProgressBounds.Contains(point);

    private void CycleVisualisation()
    {
        visualisationIndex = (visualisationIndex + 1) % visualisations.Length;
        player.State.Configuration.Visualisation = visualisationIndex;
        player.State.Configuration.Save();
        InvalidateVisual();
    }

    private void SeekTo(Point point)
    {
        player.Seek(ToDesignPoint(point).X / 400.0);
    }
}
