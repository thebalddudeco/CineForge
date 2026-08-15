using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CineForge.Desktop;

/// <summary>
/// A native, stationary atmospheric wash with sub-visible ordered dithering.
/// WPF's broad transparent gradients can expose 8-bit bands on dark displays;
/// this layer computes the same illumination as pixels and gently alternates
/// neighboring alpha values so the transition remains visually continuous.
/// </summary>
public sealed class DitheredAtmosphereLayer : FrameworkElement
{
    private ImageSource? _surface;
    private int _surfaceWidth;
    private int _surfaceHeight;

    public DitheredAtmosphereLayer()
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _surface = null;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var width = Math.Clamp((int)Math.Ceiling(ActualWidth), 1, 1920);
        var height = Math.Clamp((int)Math.Ceiling(ActualHeight), 1, 1200);
        if (_surface is null || width != _surfaceWidth || height != _surfaceHeight)
        {
            _surface = BuildSurface(width, height);
            _surfaceWidth = width;
            _surfaceHeight = height;
        }

        drawingContext.DrawImage(_surface, new Rect(RenderSize));
    }

    private static ImageSource BuildSurface(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        var centerX = width * .72;
        var centerY = height * -.18;
        var radiusX = Math.Max(1, width * .62);
        var radiusY = Math.Max(1, height * .58);
        var random = new Random(4040);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = (x - centerX) / radiusX;
                var dy = (y - centerY) / radiusY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var light = Math.Clamp(1 - distance, 0, 1);
                light = light * light * (3 - 2 * light);

                // A single alpha step of stationary dither breaks visible
                // contour bands without reading as additional film grain.
                var alpha = (int)Math.Round(54 * light);
                if (alpha > 0 && random.NextDouble() < light) alpha += random.Next(0, 2);
                alpha = Math.Clamp(alpha, 0, 56);

                var offset = (y * width + x) * 4;
                pixels[offset] = (byte)(36 * alpha / 255);
                pixels[offset + 1] = (byte)(36 * alpha / 255);
                pixels[offset + 2] = (byte)(36 * alpha / 255);
                pixels[offset + 3] = (byte)alpha;
            }
        }

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }
}
