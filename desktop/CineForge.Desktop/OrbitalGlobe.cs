using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CineForge.Desktop;

/// <summary>
/// Plays the approved orbital-globe motion source inside native WPF chrome.
/// The source frames are presented directly, preserving the supplied GIF's
/// topology, orientation, colors, and timing exactly.
/// </summary>
public sealed class OrbitalGlobe : FrameworkElement
{
    public static readonly DependencyProperty ActivityProperty = DependencyProperty.Register(
        nameof(Activity), typeof(double), typeof(OrbitalGlobe),
        new FrameworkPropertyMetadata(0d));

    private readonly List<BitmapSource> _frames = [];
    private readonly List<TimeSpan> _delays = [];
    private readonly DispatcherTimer _timer;
    private int _frameIndex;

    public double Activity
    {
        get => (double)GetValue(ActivityProperty);
        set => SetValue(ActivityProperty, Math.Clamp(value, 0, 100));
    }

    public OrbitalGlobe()
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        LoadFrames();
        _timer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher)
        {
            Interval = _delays.Count > 0 ? _delays[0] : TimeSpan.FromMilliseconds(70)
        };
        _timer.Tick += AdvanceFrame;
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    private void LoadFrames()
    {
        try
        {
            var resource = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/orbital-globe.gif", UriKind.Absolute));
            if (resource is null) return;

            using var stream = resource.Stream;
            var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0) return;

            // The supplied GIF is optimized: frame zero is the full 1440x1080
            // canvas and later frames are smaller patches with placement
            // metadata. Rebuild each complete animation frame before display.
            var patches = decoder.Frames.Skip(1).ToList();
            var left = patches.Count > 0 ? patches.Min(f => ReadMetadataInt(f, "/imgdesc/Left", 0)) : 0;
            var top = patches.Count > 0 ? patches.Min(f => ReadMetadataInt(f, "/imgdesc/Top", 0)) : 0;
            var right = patches.Count > 0
                ? patches.Max(f => ReadMetadataInt(f, "/imgdesc/Left", left) + f.PixelWidth)
                : decoder.Frames[0].PixelWidth;
            var bottom = patches.Count > 0
                ? patches.Max(f => ReadMetadataInt(f, "/imgdesc/Top", top) + f.PixelHeight)
                : decoder.Frames[0].PixelHeight;

            var region = new Int32Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
            const int renderSize = 192;
            BitmapSource composite = RenderFrame(null, decoder.Frames[0], region, region, renderSize);
            _frames.Add(composite);
            _delays.Add(ReadDelay(decoder.Frames[0]));

            foreach (var frame in patches)
            {
                var placement = new Int32Rect(
                    ReadMetadataInt(frame, "/imgdesc/Left", region.X),
                    ReadMetadataInt(frame, "/imgdesc/Top", region.Y),
                    frame.PixelWidth,
                    frame.PixelHeight);
                composite = RenderFrame(composite, frame, placement, region, renderSize);
                _frames.Add(composite);
                _delays.Add(ReadDelay(frame));
            }
        }
        catch
        {
            // A decorative status instrument must never prevent CineForge
            // from opening if Windows cannot decode a particular GIF frame.
            _frames.Clear();
            _delays.Clear();
        }
    }

    private static BitmapSource RenderFrame(
        BitmapSource? previous,
        BitmapSource patch,
        Int32Rect placement,
        Int32Rect region,
        int renderSize)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            if (previous is not null)
                dc.DrawImage(previous, new Rect(0, 0, renderSize, renderSize));

            BitmapSource source = patch;
            Rect destination;
            if (previous is null)
            {
                source = new CroppedBitmap(patch, region);
                destination = new Rect(0, 0, renderSize, renderSize);
            }
            else
            {
                var scaleX = renderSize / (double)region.Width;
                var scaleY = renderSize / (double)region.Height;
                destination = new Rect(
                    (placement.X - region.X) * scaleX,
                    (placement.Y - region.Y) * scaleY,
                    placement.Width * scaleX,
                    placement.Height * scaleY);
            }
            dc.DrawImage(source, destination);
        }

        var bitmap = new RenderTargetBitmap(renderSize, renderSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static int ReadMetadataInt(BitmapFrame frame, string query, int fallback)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata metadata)
            {
                var value = metadata.GetQuery(query);
                return value switch
                {
                    byte number => number,
                    ushort number => number,
                    uint number => checked((int)number),
                    short number => number,
                    int number => number,
                    _ => fallback
                };
            }
        }
        catch { /* Missing placement metadata uses the supplied fallback. */ }
        return fallback;
    }

    private static TimeSpan ReadDelay(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata metadata &&
                metadata.GetQuery("/grctlext/Delay") is ushort centiseconds)
            {
                return TimeSpan.FromMilliseconds(Math.Max(20, centiseconds * 10));
            }
        }
        catch { /* A malformed delay falls back to the prototype cadence. */ }
        return TimeSpan.FromMilliseconds(70);
    }

    private void AdvanceFrame(object? sender, EventArgs e)
    {
        if (_frames.Count == 0) return;
        _frameIndex = (_frameIndex + 1) % _frames.Count;
        _timer.Interval = _delays[_frameIndex];
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_frames.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;

        // Leave a restrained safety margin around the supplied animation so
        // its outer wireframe never touches the header viewport edges.
        var size = (Math.Min(ActualWidth, ActualHeight) - 2) * 0.86;
        var destination = new Rect(
            (ActualWidth - size) / 2,
            (ActualHeight - size) / 2,
            size,
            size);
        drawingContext.DrawImage(_frames[_frameIndex], destination);
    }
}
