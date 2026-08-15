using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CineForge.Desktop;

public enum BevelProfile
{
    AllCorners,
    DiagonalPair
}

/// <summary>
/// Pixel-precise CineForge instrument frame. Unlike stretched Path geometry,
/// the bevel remains the same physical size at every panel dimension.
/// </summary>
public sealed class BevelChrome : Decorator
{
    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
        nameof(Padding), typeof(Thickness), typeof(BevelChrome),
        new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background), typeof(Brush), typeof(BevelChrome),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
        nameof(BorderBrush), typeof(Brush), typeof(BevelChrome),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty InnerBorderBrushProperty = DependencyProperty.Register(
        nameof(InnerBorderBrush), typeof(Brush), typeof(BevelChrome),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(BevelChrome),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CornerCutProperty = DependencyProperty.Register(
        nameof(CornerCut), typeof(double), typeof(BevelChrome),
        new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(
        nameof(Profile), typeof(BevelProfile), typeof(BevelChrome),
        new FrameworkPropertyMetadata(BevelProfile.AllCorners, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowInnerBorderProperty = DependencyProperty.Register(
        nameof(ShowInnerBorder), typeof(bool), typeof(BevelChrome),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowTelemetryProperty = DependencyProperty.Register(
        nameof(ShowTelemetry), typeof(bool), typeof(BevelChrome),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowInstrumentationProperty = DependencyProperty.Register(
        nameof(ShowInstrumentation), typeof(bool), typeof(BevelChrome),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineCornerCutsProperty = DependencyProperty.Register(
        nameof(OutlineCornerCuts), typeof(bool), typeof(BevelChrome),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    public Brush Background { get => (Brush)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public Brush BorderBrush { get => (Brush)GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Brush InnerBorderBrush { get => (Brush)GetValue(InnerBorderBrushProperty); set => SetValue(InnerBorderBrushProperty, value); }
    public Brush AccentBrush { get => (Brush)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }
    public double CornerCut { get => (double)GetValue(CornerCutProperty); set => SetValue(CornerCutProperty, value); }
    public BevelProfile Profile { get => (BevelProfile)GetValue(ProfileProperty); set => SetValue(ProfileProperty, value); }
    public bool ShowInnerBorder { get => (bool)GetValue(ShowInnerBorderProperty); set => SetValue(ShowInnerBorderProperty, value); }
    public bool ShowTelemetry { get => (bool)GetValue(ShowTelemetryProperty); set => SetValue(ShowTelemetryProperty, value); }
    public bool ShowInstrumentation { get => (bool)GetValue(ShowInstrumentationProperty); set => SetValue(ShowInstrumentationProperty, value); }
    public bool OutlineCornerCuts { get => (bool)GetValue(OutlineCornerCutsProperty); set => SetValue(OutlineCornerCutsProperty, value); }

    public BevelChrome() => SnapsToDevicePixels = true;

    protected override Size MeasureOverride(Size constraint)
    {
        var horizontal = Padding.Left + Padding.Right;
        var vertical = Padding.Top + Padding.Bottom;
        var childConstraint = new Size(
            Math.Max(0, constraint.Width - horizontal),
            Math.Max(0, constraint.Height - vertical));
        Child?.Measure(childConstraint);
        return new Size((Child?.DesiredSize.Width ?? 0) + horizontal, (Child?.DesiredSize.Height ?? 0) + vertical);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Child?.Arrange(new Rect(
            Padding.Left,
            Padding.Top,
            Math.Max(0, arrangeSize.Width - Padding.Left - Padding.Right),
            Math.Max(0, arrangeSize.Height - Padding.Top - Padding.Bottom)));
        return arrangeSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 1 || height <= 1) return;

        var cut = Math.Clamp(CornerCut, 0, Math.Min(width, height) / 3);
        var outer = CreateFrame(0.5, width - 0.5, height - 0.5, cut, Profile);
        drawingContext.DrawGeometry(Background, OutlineCornerCuts ? new Pen(BorderBrush, 1) : null, outer);
        if (!OutlineCornerCuts)
        {
            DrawOpenCutBorder(drawingContext, new Pen(BorderBrush, 1), width, height, cut, Profile);
        }

        if (ShowInnerBorder && width > 20 && height > 20)
        {
            const double inset = 8.5;
            var inner = CreateFrame(inset, width - inset, height - inset, Math.Max(3, cut - 4), Profile);
            drawingContext.DrawGeometry(null, new Pen(InnerBorderBrush, 0.7), inner);
        }

        if (!ShowTelemetry) return;
        var accentPen = new Pen(AccentBrush, 1.2);
        drawingContext.DrawLine(accentPen, new Point(10, 10.5), new Point(38, 10.5));

        if (!ShowInstrumentation) return;

        // Restrained micrographic instrumentation derived from the approved
        // CineForge concept board. These marks remain structural and never
        // compete with the panel's content.
        var faintPen = new Pen(InnerBorderBrush, .75);
        for (var index = 0; index < 5; index++)
        {
            var x = width - 73 + index * 12;
            drawingContext.DrawLine(index is 1 or 2 ? accentPen : faintPen, new Point(x, 10.5), new Point(x + 7, 10.5));
        }
        var tickTop = Math.Max(36, height * .42);
        for (var index = 0; index < 5; index++)
        {
            var length = index is 1 or 2 ? 9 : 5;
            drawingContext.DrawLine(index == 2 ? accentPen : faintPen,
                new Point(width - 10.5 - length, tickTop + index * 7),
                new Point(width - 10.5, tickTop + index * 7));
        }
        var hatchY = height - 10.5;
        for (var index = 0; index < 7; index++)
        {
            var x = 34 + index * 8;
            drawingContext.DrawLine(index is 3 or 4 ? accentPen : faintPen,
                new Point(x, hatchY), new Point(x + 5, hatchY - 5));
        }
    }

    private static void DrawOpenCutBorder(
        DrawingContext drawingContext,
        Pen pen,
        double width,
        double height,
        double cut,
        BevelProfile profile)
    {
        const double edge = 0.5;
        var right = width - edge;
        var bottom = height - edge;

        // Mirrors the original CSS clip-path treatment: the rectangular rules
        // terminate at each clipped corner instead of tracing the diagonal.
        var allCorners = profile == BevelProfile.AllCorners;
        drawingContext.DrawLine(pen, new Point(edge + cut, edge),
            new Point(allCorners ? right - cut : right, edge));
        drawingContext.DrawLine(pen, new Point(right, allCorners ? edge + cut : edge),
            new Point(right, bottom - cut));
        drawingContext.DrawLine(pen, new Point(right - cut, bottom),
            new Point(allCorners ? edge + cut : edge, bottom));
        drawingContext.DrawLine(pen, new Point(edge, allCorners ? bottom - cut : bottom),
            new Point(edge, edge + cut));
    }

    private static StreamGeometry CreateFrame(double inset, double right, double bottom, double cut, BevelProfile profile)
    {
        var left = inset;
        var top = inset;
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(left + cut, top), isFilled: true, isClosed: true);
        if (profile == BevelProfile.DiagonalPair)
        {
            // Exact v0.1.0 micro-UI silhouette:
            // clipped upper-left and lower-right, square on the opposing corners.
            context.LineTo(new Point(right, top), true, false);
            context.LineTo(new Point(right, bottom - cut), true, false);
            context.LineTo(new Point(right - cut, bottom), true, false);
            context.LineTo(new Point(left, bottom), true, false);
            context.LineTo(new Point(left, top + cut), true, false);
        }
        else
        {
            context.LineTo(new Point(right - cut, top), true, false);
            context.LineTo(new Point(right, top + cut), true, false);
            context.LineTo(new Point(right, bottom - cut), true, false);
            context.LineTo(new Point(right - cut, bottom), true, false);
            context.LineTo(new Point(left + cut, bottom), true, false);
            context.LineTo(new Point(left, bottom - cut), true, false);
            context.LineTo(new Point(left, top + cut), true, false);
        }
        geometry.Freeze();
        return geometry;
    }
}
