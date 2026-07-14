using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DJPad.Core.Utils;
using DJPad.Core.Vis;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace DJPad.Experimental;

public sealed class MiniPlayerView : Control
{
    private static readonly DJPad.Types.ColorPalette UnknownPalette = global::Resources.Resources.Unknown.GetPalette();
    private static readonly Rect OpenBounds = new(15, 15, 20, 20);
    private static readonly Rect PreviousBounds = new(145, 5, 40, 40);
    private static readonly Rect PlayBounds = new(180, 0, 50, 50);
    private static readonly Rect NextBounds = new(225, 5, 40, 40);
    private static readonly Rect CloseBounds = new(382, 2, 16, 16);
    private static readonly Rect ProgressBounds = new(50, 42, 350, 8);

    private readonly Dictionary<string, Bitmap> assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly PlayerController player;
    private readonly Oscilloscope visualisation = new();
    private readonly BitmapBuffer visualisationBuffer = new();
    private DrawingBitmap? albumArtSource;
    private Bitmap? albumArtImage;
    private DJPad.Types.ColorPalette? albumArtPalette;
    private readonly DispatcherTimer timer;
    private bool seeking;

    public MiniPlayerView(PlayerController player)
    {
        this.player = player;
        ClipToBounds = true;
        player.Changed += OnPlayerChanged;
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render,
            (_, _) => InvalidateVisual());
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

    public event Action? ExpandRequested;
    public event Action? OpenRequested;
    public event Action? CloseRequested;
    public event Action<PointerPressedEventArgs>? DragRequested;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scaleX = Bounds.Width / 400.0;
        var scaleY = Bounds.Height / 50.0;
        using (context.PushTransform(Matrix.CreateScale(scaleX, scaleY)))
        {
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(20, 20, 20)), new Rect(0, 0, 400, 50));
            DrawVisualisation(context);
            DrawAlbumArt(context);
            DrawTrack(context);
            DrawProgress(context);
            DrawAsset(context, "Folder_White.png", OpenBounds);
            if (!player.State.Playlist.Start) DrawAsset(context, "Player Previous.png", PreviousBounds);
            if (player.State.CanPlay) DrawAsset(context, player.IsPlaying ? "player pause.png" : "player play.png", PlayBounds);
            if (!player.State.Playlist.End) DrawAsset(context, "Player Next.png", NextBounds);
            DrawAsset(context, "Cancel.png", CloseBounds);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = ToDesignPoint(e.GetPosition(this));
        if (e.ClickCount == 2 && !IsActionPoint(point))
        {
            ExpandRequested?.Invoke();
            e.Handled = true;
            return;
        }

        if (CloseBounds.Contains(point)) CloseRequested?.Invoke();
        else if (OpenBounds.Contains(point)) OpenRequested?.Invoke();
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

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (seeking)
        {
            SeekTo(e.GetPosition(this));
        }
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (seeking)
        {
            SeekTo(e.GetPosition(this));
            seeking = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
        base.OnPointerReleased(e);
    }

    private void DrawAlbumArt(DrawingContext context)
    {
        var image = GetAlbumArtImage();
        if (image != null)
        {
            context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), new Rect(0, 0, 50, 50));
            return;
        }
        DrawAsset(context, "unknown.jpg", new Rect(0, 0, 50, 50));
    }

    private void DrawVisualisation(DrawingContext context)
    {
        if (!player.State.CanPlay)
        {
            return;
        }

        visualisation.SampleSource = player.State.Audio;
        var palette = GetPalette();
        var frame = visualisation.Draw(new System.Drawing.Size(350, 50), DrawingColor.Transparent,
            player.IsPlaying, player.Current?.Source.Duration, palette);
        if (frame == null)
        {
            return;
        }

        var image = visualisationBuffer.Update(frame);
        context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), new Rect(50, 0, 350, 50));
    }

    private void DrawTrack(DrawingContext context)
    {
        var item = player.Current;
        if (item == null)
        {
            return;
        }

        var metadata = item.Metadata;
        var title = new FormattedText(metadata.Title ?? item.FileName,
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), 10, Brushes.White);
        var titleOutline = new FormattedText(metadata.Title ?? item.FileName,
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), 10, Brushes.SlateGray);
        var details = new FormattedText($"{metadata.Album}\n{metadata.Artist}",
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Light", FontStyle.Italic), 8, Brushes.LightGray);
        var detailsOutline = new FormattedText($"{metadata.Album}\n{metadata.Artist}",
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Light", FontStyle.Italic), 8, Brushes.SlateGray);
        using (context.PushClip(new Rect(56, 1, 322, 47)))
        {
            DrawOutlinedText(context, title, titleOutline, new Point(56, 1));
            DrawOutlinedText(context, details, detailsOutline, new Point(56, 14));
        }

        var position = new FormattedText(item.Source.Position.ToString(@"m\:ss"),
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold), 9, Brushes.White);
        var positionOutline = new FormattedText(item.Source.Position.ToString(@"m\:ss"),
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold), 9, Brushes.SlateGray);
        DrawOutlinedText(context, position, positionOutline, new Point(378 - position.Width, 35));
    }

    private void DrawProgress(DrawingContext context)
    {
        if (player.Current?.Source == null)
        {
            return;
        }

        var duration = player.Current.Source.Duration.TotalSeconds;
        var progress = duration <= 0 ? 0 : player.Current.Source.Position.TotalSeconds / duration;
        context.DrawLine(new Pen(Brushes.White, 1), new Point(50, 49), new Point(400, 49));
        var x = 50 + (Math.Clamp(progress, 0, 1) * 350);
        context.DrawLine(new Pen(Brushes.White, 4), new Point(x, 42), new Point(x, 50));
    }

    private void DrawAsset(DrawingContext context, string name, Rect destination)
    {
        if (!assets.TryGetValue(name, out var image))
        {
            using var stream = AssetLoader.Open(new Uri($"avares://DJPad.Experimental/Assets/{Uri.EscapeDataString(name)}"));
            image = new Bitmap(stream);
            assets[name] = image;
        }
        context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), destination);
    }

    private void OnPlayerChanged()
    {
        Dispatcher.UIThread.Post(InvalidateVisual);
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
            albumArtImage?.Dispose();
            albumArtSource = source;
            albumArtImage = ConvertBitmap(source);
            albumArtPalette = source.GetPalette();
        }
        return albumArtImage;
    }

    private DJPad.Types.ColorPalette GetPalette()
    {
        if (player.Current?.SynchronousArt == null)
        {
            return UnknownPalette;
        }
        GetAlbumArtImage();
        return albumArtPalette ?? UnknownPalette;
    }

    private void SeekTo(Point point)
    {
        player.Seek((ToDesignPoint(point).X - ProgressBounds.X) / ProgressBounds.Width);
    }

    private Point ToDesignPoint(Point point)
    {
        return new Point(point.X * 400 / Bounds.Width, point.Y * 50 / Bounds.Height);
    }

    private static bool IsActionPoint(Point point)
    {
        return OpenBounds.Contains(point) || PreviousBounds.Contains(point) || PlayBounds.Contains(point)
            || NextBounds.Contains(point) || CloseBounds.Contains(point) || ProgressBounds.Contains(point);
    }

    private static Bitmap ConvertBitmap(DrawingBitmap source)
    {
        using var stream = new MemoryStream();
        source.Save(stream, DrawingImageFormat.Png);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    private static void DrawOutlinedText(DrawingContext context, FormattedText text, FormattedText outline, Point position)
    {
        context.DrawText(outline, position + new Vector(-1, 0));
        context.DrawText(outline, position + new Vector(1, 0));
        context.DrawText(outline, position + new Vector(0, -1));
        context.DrawText(outline, position + new Vector(0, 1));
        context.DrawText(text, position);
    }
}
