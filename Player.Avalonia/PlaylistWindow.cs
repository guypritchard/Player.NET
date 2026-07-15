using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DJPad.Core;
using DJPad.Core.Interfaces;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace DJPad.Experimental;

public sealed class PlaylistWindow : Window
{
    public PlaylistWindow(PlayerController player)
    {
        Title = "Playlist";
        Width = 400;
        Height = 400;
        MinWidth = 400;
        MinHeight = 400;
        MaxWidth = 400;
        MaxHeight = 400;
        CanResize = false;
        ShowInTaskbar = false;
        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Black;
        Content = new PlaylistView(player);
    }
}

internal sealed class PlaylistView : Control
{
    private const int VisibleRows = 8;
    private readonly Dictionary<IPlaylistItem, Bitmap> artwork = new();
    private readonly PlayerController player;
    private Bitmap? unknownArtwork;
    private IPlaylistItem? displayedCurrent;
    private int scrollPosition;

    public PlaylistView(PlayerController player)
    {
        this.player = player;
        displayedCurrent = player.Current;
        PointerWheelChanged += OnPointerWheelChanged;
        AttachedToVisualTree += (_, _) => player.Changed += Refresh;
        DetachedFromVisualTree += (_, _) =>
        {
            player.Changed -= Refresh;
            foreach (var image in artwork.Values)
            {
                image.Dispose();
            }
            artwork.Clear();
            unknownArtwork?.Dispose();
            unknownArtwork = null;
        };
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
        var tracks = player.Tracks;
        var rowHeight = Bounds.Height / VisibleRows;
        for (var row = 0; row < VisibleRows; row++)
        {
            var index = scrollPosition + row;
            if (index >= tracks.Count)
            {
                break;
            }

            DrawRow(context, tracks[index], index, row * rowHeight, rowHeight);
        }

        if (tracks.Count > 0)
        {
            var current = player.CurrentIndex;
            var currentY = Math.Clamp(current / (double)tracks.Count * 400, 0, 390);
            var scrollY = Math.Clamp(scrollPosition / (double)tracks.Count * 400, 0, 390);
            context.DrawLine(new Pen(Brushes.DarkGray), new Point(397, Math.Min(currentY, scrollY)), new Point(397, Math.Max(currentY, scrollY)));
            context.FillRectangle(Brushes.White, new Rect(395, currentY, 5, 10));
            context.FillRectangle(Brushes.White, new Rect(395, scrollY, 5, 10));
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (e.ClickCount == 2)
        {
            var rowHeight = Bounds.Height / VisibleRows;
            var index = scrollPosition + (int)(point.Y / rowHeight);
            if (index >= 0 && index < player.Tracks.Count)
            {
                player.Play(player.Tracks[index]);
                e.Handled = true;
            }
        }
        base.OnPointerPressed(e);
    }

    private void DrawRow(DrawingContext context, IPlaylistItem item, int index, double y, double rowHeight)
    {
        if (item == player.Current)
        {
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(55, 128, 128, 128)), new Rect(0, y, 395, rowHeight));
            context.FillRectangle(Brushes.White, new Rect(50, y, 3, rowHeight));
        }

        var image = GetArtwork(item);
        context.DrawImage(image, new Rect(0, 0, image.Size.Width, image.Size.Height), new Rect(0, y, 50, rowHeight));

        var number = new FormattedText($"{index + 1}.", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold), 13, Brushes.SlateGray);
        context.DrawText(number, new Point(54, y + 1));

        var metadata = item.Metadata;
        var title = new FormattedText(metadata.Title ?? item.FileName, System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Normal, FontWeight.Bold), 13, Brushes.White);
        context.DrawText(title, new Point(82, y + 1));

        var details = new FormattedText($"{metadata.Album}\n{metadata.Artist}", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI Light", FontStyle.Italic), 11, Brushes.Azure);
        context.DrawText(details, new Point(82, y + 18));

        var duration = new FormattedText(metadata.Duration.ToString(@"m\:ss"), System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold), 13, Brushes.White);
        context.DrawText(duration, new Point(390 - duration.Width, y + 1));
    }

    private Bitmap GetArtwork(IPlaylistItem item)
    {
        var embeddedArtwork = item.HasLoadedMetadata
            ? (item.Metadata as IEmbeddedArtworkMetadata)?.EmbeddedAlbumArt
            : null;
        if (embeddedArtwork != null)
        {
            if (!artwork.TryGetValue(item, out var image))
            {
                image = ConvertBitmap(embeddedArtwork);
                artwork[item] = image;
            }
            return image;
        }

        if (unknownArtwork == null)
        {
            using var stream = AssetLoader.Open(new Uri("avares://Player.NET/Assets/unknown.jpg"));
            unknownArtwork = new Bitmap(stream);
        }
        return unknownArtwork;
    }

    private static Bitmap ConvertBitmap(DrawingBitmap source)
    {
        using var stream = new MemoryStream();
        source.Save(stream, DrawingImageFormat.Png);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var max = Math.Max(0, player.Tracks.Count - VisibleRows);
        scrollPosition = Math.Clamp(scrollPosition + (e.Delta.Y > 0 ? -1 : 1), 0, max);
        InvalidateVisual();
        e.Handled = true;
    }

    private void Refresh()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var currentItem = player.Current;
            if (!ReferenceEquals(currentItem, displayedCurrent))
            {
                displayedCurrent = currentItem;
                var current = player.CurrentIndex;
                if (current >= 0 && (current < scrollPosition || current >= scrollPosition + VisibleRows))
                {
                    scrollPosition = Math.Clamp(current - VisibleRows / 2, 0, Math.Max(0, player.Tracks.Count - VisibleRows));
                }
            }
            InvalidateVisual();
        });
    }
}
