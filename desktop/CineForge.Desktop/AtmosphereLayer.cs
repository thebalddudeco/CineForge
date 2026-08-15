using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CineForge.Desktop;

/// <summary>
/// Native version of the v0.1 film-grain layer. The texture is generated in
/// memory and tiled across the client area as a quiet, stationary surface.
/// Grain is texture, not telemetry, so it must never animate.
/// </summary>
public sealed class AtmosphereLayer : FrameworkElement
{
    private readonly ImageBrush _grain;
    private readonly DrawingBrush _scanlines;

    public AtmosphereLayer()
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
        _grain = CreateGrainBrush();
        _scanlines = CreateScanlineBrush();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(_scanlines, null, new Rect(RenderSize));
        drawingContext.DrawRectangle(_grain, null, new Rect(RenderSize));
    }

    private static ImageBrush CreateGrainBrush()
    {
        const int size = 180;
        var pixels = new byte[size * size * 4];
        var random = new Random(861);
        for (var index = 0; index < size * size; index++)
        {
            var offset = index * 4;
            var light = random.NextDouble() > .51;
            pixels[offset] = light ? (byte)224 : (byte)2;
            pixels[offset + 1] = light ? (byte)224 : (byte)3;
            pixels[offset + 2] = light ? (byte)224 : (byte)0;
            pixels[offset + 3] = (byte)random.Next(2, 16);
        }

        var bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        bitmap.Freeze();
        return new ImageBrush(bitmap)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, size, size),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            // Approved static texture, reduced 35% from the previous .78 intensity.
            Opacity = .507
        };
    }

    private static DrawingBrush CreateScanlineBrush()
    {
        var drawing = new GeometryDrawing(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(7, 224, 224, 224)), .5),
            Geometry.Parse("M0,3 H8"));
        drawing.Freeze();
        return new DrawingBrush(drawing)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 8, 4),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None
        };
    }
}
