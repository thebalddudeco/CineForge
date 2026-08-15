using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CineForge.Installer;

internal static class CineForgeTheme
{
    internal static readonly Color Chartreuse = Color.FromArgb(228, 255, 26);
    internal static readonly Color Lime = Color.FromArgb(137, 252, 0);
    internal static readonly Color Carbon = Color.FromArgb(36, 36, 36);
    internal static readonly Color Black = Color.FromArgb(2, 3, 0);
    internal static readonly Color Alabaster = Color.FromArgb(224, 224, 224);
    internal static readonly Color Muted = Color.FromArgb(142, 142, 138);
    internal static readonly Color Line = Color.FromArgb(76, 77, 72);

    private static readonly PrivateFontCollection Fonts = new();
    private static readonly List<IntPtr> FontBuffers = [];

    static CineForgeTheme()
    {
        LoadFont("CineForge.Installer.Fonts.Anta-Regular.ttf");
        LoadFont("CineForge.Installer.Fonts.SairaCondensed-Regular.ttf");
        LoadFont("CineForge.Installer.Fonts.CutiveMono-Regular.ttf");
        LoadFont("CineForge.Installer.Fonts.InterTight-VariableFont_wght.ttf");
    }

    internal static Font Title(float size, FontStyle style = FontStyle.Regular) => FontNamed("Anta", "Segoe UI", size, style);
    internal static Font Control(float size, FontStyle style = FontStyle.Regular) => FontNamed("Saira Condensed", "Segoe UI", size, style);
    internal static Font Mono(float size, FontStyle style = FontStyle.Regular) => FontNamed("Cutive Mono", "Consolas", size, style);
    internal static Font Body(float size, FontStyle style = FontStyle.Regular) => FontNamed("Inter Tight", "Segoe UI", size, style);

    private static Font FontNamed(string familyName, string fallback, float size, FontStyle style)
    {
        try
        {
            FontFamily? family = Fonts.Families.FirstOrDefault(candidate => candidate.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
            if (family is not null) return new Font(family, size, style, GraphicsUnit.Point);
        }
        catch { }
        return new Font(fallback, size, style, GraphicsUnit.Point);
    }

    private static void LoadFont(string resourceName)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null) return;
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        IntPtr buffer = Marshal.AllocCoTaskMem(bytes.Length);
        Marshal.Copy(bytes, 0, buffer, bytes.Length);
        Fonts.AddMemoryFont(buffer, bytes.Length);
        FontBuffers.Add(buffer);
    }

    internal static Point[] ClippedRectangle(Rectangle bounds, int cut = 12) =>
    [
        new(bounds.Left + cut, bounds.Top), new(bounds.Right - cut, bounds.Top),
        new(bounds.Right, bounds.Top + cut), new(bounds.Right, bounds.Bottom - cut),
        new(bounds.Right - cut, bounds.Bottom), new(bounds.Left + cut, bounds.Bottom),
        new(bounds.Left, bounds.Bottom - cut), new(bounds.Left, bounds.Top + cut)
    ];
}

internal sealed class CineForgeSelector : Control
{
    private readonly List<LanguageOption> options = [];
    private string selectedId = "en";

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string SelectedValue => selectedId;

    internal CineForgeSelector()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        Font = CineForgeTheme.Body(9);
        BackColor = CineForgeTheme.Black;
        ForeColor = CineForgeTheme.Alabaster;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    internal void SetOptions(IEnumerable<LanguageOption> values, string defaultId)
    {
        options.Clear();
        options.AddRange(values);
        selectedId = options.Any(option => option.Id == defaultId) ? defaultId : options[0].Id;
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            BackColor = CineForgeTheme.Carbon,
            ForeColor = CineForgeTheme.Alabaster,
            Font = CineForgeTheme.Body(9),
            Renderer = new CineForgeMenuRenderer(),
            Width = Width
        };
        foreach (LanguageOption option in options)
        {
            var item = new ToolStripMenuItem(option.Label) { Tag = option.Id, AutoSize = false, Width = Width - 4, Height = 30 };
            item.Click += (_, _) => { selectedId = option.Id; Invalidate(); };
            menu.Items.Add(item);
        }
        menu.Show(this, new Point(0, Height));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space or Keys.Down)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Enabled ? CineForgeTheme.Black : CineForgeTheme.Carbon);
        using var border = new Pen(Focused ? CineForgeTheme.Chartreuse : CineForgeTheme.Line);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        LanguageOption? option = options.FirstOrDefault(value => value.Id == selectedId);
        TextRenderer.DrawText(e.Graphics, option?.Label ?? string.Empty, Font, new Rectangle(10, 0, Width - 38, Height),
            Enabled ? CineForgeTheme.Alabaster : CineForgeTheme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        using var caret = new Pen(CineForgeTheme.Chartreuse, 1.4f);
        int cx = Width - 17;
        int cy = Height / 2;
        e.Graphics.DrawLine(caret, cx - 4, cy - 2, cx, cy + 2);
        e.Graphics.DrawLine(caret, cx, cy + 2, cx + 4, cy - 2);
    }
}

internal sealed class CineForgeMenuRenderer : ToolStripProfessionalRenderer
{
    internal CineForgeMenuRenderer() : base(new CineForgeColorTable()) { RoundedEdges = false; }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? CineForgeTheme.Black : CineForgeTheme.Alabaster;
        base.OnRenderItemText(e);
    }
}

internal sealed class CineForgeColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => CineForgeTheme.Carbon;
    public override Color MenuBorder => CineForgeTheme.Chartreuse;
    public override Color MenuItemBorder => CineForgeTheme.Chartreuse;
    public override Color MenuItemSelected => CineForgeTheme.Chartreuse;
    public override Color MenuItemSelectedGradientBegin => CineForgeTheme.Chartreuse;
    public override Color MenuItemSelectedGradientEnd => CineForgeTheme.Chartreuse;
    public override Color ImageMarginGradientBegin => CineForgeTheme.Carbon;
    public override Color ImageMarginGradientMiddle => CineForgeTheme.Carbon;
    public override Color ImageMarginGradientEnd => CineForgeTheme.Carbon;
}

internal sealed class InstallerSurface : Panel
{
    internal InstallerSurface()
    {
        DoubleBuffered = true;
        BackColor = CineForgeTheme.Black;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(CineForgeTheme.Black);
        using var minor = new Pen(Color.FromArgb(18, CineForgeTheme.Alabaster));
        using var major = new Pen(Color.FromArgb(28, CineForgeTheme.Alabaster));
        for (int x = 0; x < Width; x += 20) e.Graphics.DrawLine(x % 100 == 0 ? major : minor, x, 0, x, Height);
        for (int y = 0; y < Height; y += 20) e.Graphics.DrawLine(y % 100 == 0 ? major : minor, 0, y, Width, y);

        // Fixed deterministic grain: texture without distracting animation.
        var random = new Random(501);
        using var grain = new SolidBrush(Color.FromArgb(16, CineForgeTheme.Alabaster));
        for (int i = 0; i < 850; i++) e.Graphics.FillRectangle(grain, random.Next(Width), random.Next(Height), 1, 1);
    }
}

internal sealed class InstrumentPanel : Panel
{
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool DoubleFrame { get; init; }

    internal InstrumentPanel()
    {
        DoubleBuffered = true;
        BackColor = CineForgeTheme.Black;
        Padding = new Padding(18);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle outer = new(0, 0, Width - 1, Height - 1);
        using var fill = new SolidBrush(Color.FromArgb(238, CineForgeTheme.Carbon));
        using var border = new Pen(CineForgeTheme.Line);
        e.Graphics.FillPolygon(fill, CineForgeTheme.ClippedRectangle(outer));
        e.Graphics.DrawPolygon(border, CineForgeTheme.ClippedRectangle(outer));
        if (DoubleFrame)
        {
            Rectangle inner = Rectangle.Inflate(outer, -7, -7);
            using var innerPen = new Pen(Color.FromArgb(86, CineForgeTheme.Alabaster));
            e.Graphics.DrawPolygon(innerPen, CineForgeTheme.ClippedRectangle(inner, 8));
        }
        using var active = new Pen(CineForgeTheme.Chartreuse, 2);
        e.Graphics.DrawLine(active, 18, 10, 46, 10);
        using var hatch = new Pen(CineForgeTheme.Chartreuse, 1);
        e.Graphics.DrawLine(hatch, Width - 50, 11, Width - 44, 11);
        e.Graphics.DrawLine(hatch, Width - 39, 11, Width - 33, 11);
    }
}

internal sealed class CineForgeButton : Button
{
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Primary { get; init; }
    private bool hovered;

    internal CineForgeButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = CineForgeTheme.Control(10, FontStyle.Bold);
        MouseEnter += (_, _) => { hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { hovered = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color background = Primary
            ? (hovered ? CineForgeTheme.Lime : CineForgeTheme.Chartreuse)
            : (hovered ? Color.FromArgb(54, 54, 51) : CineForgeTheme.Carbon);
        Color foreground = Primary ? CineForgeTheme.Black : CineForgeTheme.Alabaster;
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        Point[] shape = CineForgeTheme.ClippedRectangle(bounds, 7);
        using var fill = new SolidBrush(background);
        using var border = new Pen(Primary ? CineForgeTheme.Chartreuse : CineForgeTheme.Line);
        e.Graphics.FillPolygon(fill, shape);
        e.Graphics.DrawPolygon(border, shape);
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        if (Focused)
        {
            Rectangle focus = Rectangle.Inflate(bounds, -4, -4);
            using var focusPen = new Pen(Primary ? CineForgeTheme.Black : CineForgeTheme.Chartreuse) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawRectangle(focusPen, focus);
        }
    }
}

internal sealed class SegmentedProgressBar : Control
{
    private int value;
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int Value
    {
        get => value;
        set { this.value = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    internal SegmentedProgressBar()
    {
        DoubleBuffered = true;
        BackColor = CineForgeTheme.Black;
        ForeColor = CineForgeTheme.Chartreuse;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        const int segments = 52;
        const int gap = 3;
        int segmentWidth = Math.Max(3, (Width - ((segments - 1) * gap)) / segments);
        int active = (int)Math.Round(segments * (Value / 100d));
        for (int i = 0; i < segments; i++)
        {
            int x = i * (segmentWidth + gap);
            Color color = i < active ? (i == active - 1 ? CineForgeTheme.Lime : CineForgeTheme.Chartreuse) : Color.FromArgb(58, 59, 55);
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, x, 2, segmentWidth, Height - 4);
        }
    }
}

internal sealed class VersionBadge : Control
{
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string VersionText { get; init; } = "v0.5.0";

    internal VersionBadge()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int topInset = Math.Max(12, (int)Math.Round(Width * 0.24));
        int shoulderY = Math.Max(16, (int)Math.Round(Height * 0.30));
        int bottomInset = Math.Max(20, (int)Math.Round(Width * 0.36));
        Point[] badge =
        [
            new(topInset, 4),
            new(Width - topInset, 4),
            new(Width - 3, shoulderY),
            new(Width - bottomInset, Height - 5),
            new(bottomInset, Height - 5),
            new(3, shoulderY)
        ];
        using var fill = new SolidBrush(CineForgeTheme.Chartreuse);
        e.Graphics.FillPolygon(fill, badge);
        TextRenderer.DrawText(e.Graphics, VersionText, CineForgeTheme.Mono(7, FontStyle.Bold), ClientRectangle,
            CineForgeTheme.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

internal sealed class RegistrationMarks : Control
{
    internal RegistrationMarks()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.Transparent;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(150, CineForgeTheme.Alabaster));
        const int length = 12;
        e.Graphics.DrawLine(pen, 0, length, 0, 0); e.Graphics.DrawLine(pen, 0, 0, length, 0);
        e.Graphics.DrawLine(pen, Width - length, 0, Width - 1, 0); e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, length);
        e.Graphics.DrawLine(pen, 0, Height - length, 0, Height - 1); e.Graphics.DrawLine(pen, 0, Height - 1, length, Height - 1);
        e.Graphics.DrawLine(pen, Width - length, Height - 1, Width - 1, Height - 1); e.Graphics.DrawLine(pen, Width - 1, Height - 1, Width - 1, Height - length);
    }
}
