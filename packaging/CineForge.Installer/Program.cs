using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace CineForge.Installer;

internal static class Program
{
    internal const string ProductName = "CineForge Desktop";
    internal const string InstallFolderName = "CineForge";
    internal const string Publisher = "The Bald Dude Co.";
    internal static string ProductVersion => Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "5.1";
    internal static readonly string ProgramsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
    internal static readonly string DefaultInstallRoot = Path.Combine(ProgramsRoot, InstallFolderName);
    internal static readonly string DefaultDataRoot = DefaultLibraryRoot();
    internal static string DefaultLanguage => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "ko" => "ko", "ja" => "ja", _ => "en"
    };
    internal static readonly string PointerRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CineForge");
    internal static readonly string PointerPath = Path.Combine(PointerRoot, "install.json");
    internal const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CineForgeDesktop";

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--uninstall-worker", StringComparer.OrdinalIgnoreCase))
        {
            RunUninstallWorker(args.Contains("--silent", StringComparer.OrdinalIgnoreCase));
            return;
        }
        if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
        {
            LaunchUninstallWorker(args.Contains("--silent", StringComparer.OrdinalIgnoreCase));
            return;
        }
        if (args.Contains("--silent", StringComparer.OrdinalIgnoreCase))
        {
            InstallerEngine.InstallAsync(DefaultInstallRoot, DefaultDataRoot, DefaultLanguage, null, CancellationToken.None).GetAwaiter().GetResult();
            return;
        }
        Application.Run(new InstallerForm());
    }

    private static string DefaultLibraryRoot()
    {
        string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (string.IsNullOrWhiteSpace(videos)) videos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(videos, "CineForge Library");
    }

    private static void LaunchUninstallWorker(bool silent)
    {
        string tempCopy = Path.Combine(Path.GetTempPath(), $"CineForge-Uninstall-{Guid.NewGuid():N}.exe");
        File.Copy(Environment.ProcessPath!, tempCopy, true);
        string installRoot = Path.GetDirectoryName(Environment.ProcessPath!)!;
        Process.Start(new ProcessStartInfo(tempCopy, $"--uninstall-worker --install-root \"{installRoot}\"{(silent ? " --silent" : "")}") { UseShellExecute = true });
    }

    private static void RunUninstallWorker(bool silent)
    {
        string[] command = Environment.GetCommandLineArgs();
        int index = Array.FindIndex(command, value => value.Equals("--install-root", StringComparison.OrdinalIgnoreCase));
        string installRoot = index >= 0 && index + 1 < command.Length ? command[index + 1] : "";
        if (!silent)
        {
            var answer = MessageBox.Show(
                "Remove CineForge Desktop from this PC?\n\nYour CineForge Library, models, inputs, outputs, and projects will be preserved.",
                "Uninstall CineForge Desktop", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }
        try
        {
            InstallerEngine.Uninstall(installRoot);
            if (!silent) MessageBox.Show("CineForge Desktop was removed. Your CineForge Library was preserved.", "Uninstall complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show(ex.Message, "Uninstall failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            MoveFileEx(Environment.ProcessPath!, null, 0x4);
        }
    }
}

internal sealed class InstallerForm : Form
{
    private readonly Label statusLabel;
    private readonly SegmentedProgressBar progressBar;
    private readonly AngledButton installButton;
    private readonly AngledButton cancelButton;
    private readonly TextBox installPath;
    private readonly TextBox libraryPath;
    private readonly ComboBox languagePicker;
    private readonly Label signalCodeLabel;
    private readonly Label signalPercentLabel;
    private readonly Bitmap backgroundNoise;
    private CancellationTokenSource? cancellation;

    public InstallerForm()
    {
        Text = $"CineForge Desktop {Program.ProductVersion} Setup";
        DoubleBuffered = true;
        ClientSize = new Size(1160, 900);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = InstallerPalette.Black;
        ForeColor = Color.FromArgb(224, 224, 224);
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        backgroundNoise = BrandAssets.CreateNoiseTexture(ClientSize.Width, ClientSize.Height);

        var topBar = new Panel { Bounds = new Rectangle(0, 0, ClientSize.Width, 42), BackColor = Color.Transparent };
        topBar.MouseDown += DragWindow;
        Controls.Add(topBar);

        var brandMark = new PictureBox
        {
            Image = BrandAssets.IconMarkAcid,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Bounds = new Rectangle(22, 12, 26, 18)
        };
        brandMark.MouseDown += DragWindow;
        topBar.Controls.Add(brandMark);

        var chromeTitle = new Label
        {
            Text = $"CINEFORGE DESKTOP  /  SETUP SYSTEM  /  {Program.ProductVersion}",
            AutoSize = true,
            ForeColor = InstallerPalette.Alabaster,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 10, FontStyle.Regular),
            Location = new Point(58, 12)
        };
        chromeTitle.MouseDown += DragWindow;
        topBar.Controls.Add(chromeTitle);

        var minimize = CreateChromeButton("—", new Point(ClientSize.Width - 92, 8), () => WindowState = FormWindowState.Minimized);
        var close = CreateChromeButton("×", new Point(ClientSize.Width - 48, 8), Close);
        topBar.Controls.Add(minimize);
        topBar.Controls.Add(close);

        var frameMarks = new FrameMarks { Bounds = new Rectangle(26, 58, 1108, 782), BackColor = Color.Transparent };
        Controls.Add(frameMarks);

        var headerRule = new RuleLine { Bounds = new Rectangle(42, 82, 1062, 14), DotAlignedRight = true, BackColor = Color.Transparent };
        Controls.Add(headerRule);

        var eyebrow = new Label
        {
            Text = "CINEFORGE DESKTOP / LOCAL WAN VIDEO SYSTEM",
            AutoSize = true,
            ForeColor = InstallerPalette.BrandGreen,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 10, FontStyle.Bold),
            Location = new Point(58, 132)
        };
        Controls.Add(eyebrow);

        var title = new Label
        {
            Text = "Install CineForge Desktop",
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = InstallerPalette.Alabaster,
            Font = new Font("Segoe UI", 30, FontStyle.Regular),
            Location = new Point(58, 160)
        };
        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Choose the application and private local-library locations. Existing verified files are reused.",
            AutoSize = true,
            ForeColor = InstallerPalette.SoftText,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Location = new Point(60, 222)
        };
        Controls.Add(subtitle);

        var routerPanel = new NotchedPanel
        {
            Bounds = new Rectangle(42, 286, 1046, 372),
            PanelTitle = "01 / INSTALLATION ROUTER",
            PanelCode = "LOCAL / DESKTOP"
        };
        Controls.Add(routerPanel);

        var languageLabel = FieldLabel("LANGUAGE / 언어 / 言語", 26, 54);
        languagePicker = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            IntegralHeight = false,
            Bounds = new Rectangle(26, 77, 235, 30),
            DisplayMember = nameof(LanguageOption.Label),
            ValueMember = nameof(LanguageOption.Id)
        };
        ConfigureCombo(languagePicker);
        languagePicker.Items.AddRange([new LanguageOption("en", "EN  English"), new LanguageOption("ko", "한  한국어"), new LanguageOption("ja", "日  日本語")]);
        languagePicker.SelectedValue = Program.DefaultLanguage;
        languagePicker.DrawItem += DrawLanguageItem;
        routerPanel.Controls.Add(languageLabel);
        routerPanel.Controls.Add(languagePicker);

        var appLabel = FieldLabel("APPLICATION FOLDER", 26, 138);
        installPath = PathBox(26, 161, 636, Program.DefaultInstallRoot);
        var appBrowse = BrowseButton(674, 159, () => BrowseFor(installPath, "Select a parent folder for CineForge", "CineForge"));
        routerPanel.Controls.Add(appLabel);
        routerPanel.Controls.Add(installPath);
        routerPanel.Controls.Add(appBrowse);

        var libraryLabel = FieldLabel("CINEFORGE LIBRARY", 26, 214);
        libraryPath = PathBox(26, 237, 636, Program.DefaultDataRoot);
        var libraryBrowse = BrowseButton(674, 235, () => BrowseFor(libraryPath, "Select a parent folder for the CineForge Library", "CineForge Library"));
        routerPanel.Controls.Add(libraryLabel);
        routerPanel.Controls.Add(libraryPath);
        routerPanel.Controls.Add(libraryBrowse);

        var libraryNoteBack = new Panel
        {
            Bounds = new Rectangle(26, 286, 560, 40),
            BackColor = Color.FromArgb(6, 6, 6)
        };
        var libraryNote = new Label
        {
            Text = "SEPARATE DATA VAULT  /  inputs  ·  outputs  ·  projects  ·  models  ·  cache  ·  logs  ·  temp\nSetup verifies existing components before downloading the Wan pack.",
            AutoSize = false,
            Size = new Size(550, 34),
            Location = new Point(4, 2),
            ForeColor = InstallerPalette.SoftText,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
        };
        libraryNoteBack.Controls.Add(libraryNote);
        routerPanel.Controls.Add(libraryNoteBack);

        var divider = new Panel { Bounds = new Rectangle(26, 346, 970, 1), BackColor = InstallerPalette.Line };
        routerPanel.Controls.Add(divider);

        var signalPanel = new NotchedPanel
        {
            Bounds = new Rectangle(42, 686, 1046, 102),
            PanelTitle = "INSTALLATION SIGNAL",
            PanelCode = string.Empty
        };
        Controls.Add(signalPanel);

        progressBar = new SegmentedProgressBar
        {
            Bounds = new Rectangle(26, 42, 938, 14),
            Visible = true
        };
        signalPanel.Controls.Add(progressBar);

        statusLabel = new Label
        {
            Text = "READY / Awaiting installation command",
            AutoEllipsis = true,
            Bounds = new Rectangle(26, 66, 938, 20),
            ForeColor = InstallerPalette.Alabaster,
            BackColor = Color.Black,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        signalPanel.Controls.Add(statusLabel);

        signalCodeLabel = new Label
        {
            Text = $"PKG {Program.ProductVersion} / READY",
            AutoSize = true,
            ForeColor = InstallerPalette.Alabaster,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 9, FontStyle.Regular),
            Location = new Point(848, 14)
        };
        signalPanel.Controls.Add(signalCodeLabel);

        signalPercentLabel = new Label
        {
            Text = "00%",
            AutoSize = true,
            ForeColor = InstallerPalette.BrandGreen,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 11, FontStyle.Bold),
            Location = new Point(972, 40)
        };
        signalPanel.Controls.Add(signalPercentLabel);

        cancelButton = new AngledButton
        {
            Text = "PAUSE",
            Bounds = new Rectangle(628, 824, 112, 42),
            FillColor = InstallerPalette.Carbon,
            ForeColor = InstallerPalette.Alabaster,
            Visible = false
        };
        installButton = new AngledButton
        {
            Text = "INSTALL + DOWNLOAD  →",
            Bounds = new Rectangle(840, 820, 248, 48),
            FillColor = InstallerPalette.BrandGreen,
            ForeColor = InstallerPalette.DarkText
        };
        cancelButton.Click += (_, _) => cancellation?.Cancel();
        installButton.Click += InstallClicked;
        Controls.Add(cancelButton);
        Controls.Add(installButton);

        var footer = new Label
        {
            Text = "LOCAL-FIRST / VERIFIED COMPONENTS / PRIVATE RUNTIME",
            AutoSize = true,
            ForeColor = InstallerPalette.SoftText,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 9),
            Location = new Point(54, 844)
        };
        Controls.Add(footer);

        frameMarks.SendToBack();
        headerRule.BringToFront();
        eyebrow.BringToFront();
        title.BringToFront();
        subtitle.BringToFront();
        routerPanel.BringToFront();
        signalPanel.BringToFront();
        cancelButton.BringToFront();
        installButton.BringToFront();
        footer.BringToFront();
        topBar.BringToFront();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var solid = new SolidBrush(InstallerPalette.Black);
        e.Graphics.FillRectangle(solid, ClientRectangle);
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        DrawGrid(e.Graphics, ClientRectangle, 24, Color.FromArgb(28, 36, 28));
        e.Graphics.DrawImage(backgroundNoise, 0, 0, ClientSize.Width, ClientSize.Height);
    }

    private static void DrawGrid(Graphics g, Rectangle bounds, int spacing, Color lineColor)
    {
        using var pen = new Pen(lineColor, 1f);
        for (int x = bounds.Left; x < bounds.Right; x += spacing) g.DrawLine(pen, x, bounds.Top, x, bounds.Bottom);
        for (int y = bounds.Top; y < bounds.Bottom; y += spacing) g.DrawLine(pen, bounds.Left, y, bounds.Right, y);
    }

    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);
    }

    private static Label FieldLabel(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = InstallerPalette.BrandGreen,
        BackColor = Color.Transparent,
        Font = new Font("Consolas", 8.5f, FontStyle.Bold),
        Location = new Point(x, y)
    };

    private static TextBox PathBox(int x, int y, int width, string value)
    {
        var box = new TextBox
        {
            Text = value,
            Bounds = new Rectangle(x, y, width, 28),
            BackColor = Color.FromArgb(6, 6, 6),
            ForeColor = InstallerPalette.Alabaster,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        return box;
    }

    private static AngledButton BrowseButton(int x, int y, Action action)
    {
        var button = new AngledButton
        {
            Text = "BROWSE…",
            Bounds = new Rectangle(x, y, 106, 30),
            FillColor = InstallerPalette.BrandGreen,
            ForeColor = InstallerPalette.DarkText
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static Control CreateChromeButton(string text, Point location, Action click)
    {
        var button = new Label
        {
            Text = text,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            AutoSize = false,
            Bounds = new Rectangle(location.X, location.Y, 30, 24),
            ForeColor = InstallerPalette.Alabaster,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 12, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        button.Click += (_, _) => click();
        return button;
    }

    private static void ConfigureCombo(ComboBox combo)
    {
        combo.BackColor = Color.FromArgb(6, 6, 6);
        combo.ForeColor = InstallerPalette.Alabaster;
        combo.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        combo.FlatStyle = FlatStyle.Flat;
        combo.DropDownHeight = 160;
    }

    private void DrawLanguageItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0) return;
        var item = (LanguageOption)languagePicker.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var fill = new SolidBrush(selected ? InstallerPalette.BrandGreen : InstallerPalette.Carbon);
        using var textBrush = new SolidBrush(selected ? InstallerPalette.DarkText : InstallerPalette.Alabaster);
        e.Graphics.FillRectangle(fill, e.Bounds);
        e.Graphics.DrawString(item.Label, languagePicker.Font, textBrush, e.Bounds.Left + 6, e.Bounds.Top + 4);
        e.DrawFocusRectangle();
    }

    private static void BrowseFor(TextBox target, string description, string leaf)
    {
        using var dialog = new FolderBrowserDialog { Description = description, UseDescriptionForTitle = true, ShowNewFolderButton = true };
        string current = target.Text.Trim();
        if (Directory.Exists(current)) dialog.SelectedPath = current;
        if (dialog.ShowDialog() != DialogResult.OK) return;
        target.Text = Path.GetFileName(dialog.SelectedPath).Equals(leaf, StringComparison.OrdinalIgnoreCase)
            ? dialog.SelectedPath : Path.Combine(dialog.SelectedPath, leaf);
    }

    private async void InstallClicked(object? sender, EventArgs e)
    {
        installButton.Enabled = false;
        installPath.Enabled = false;
        libraryPath.Enabled = false;
        languagePicker.Enabled = false;
        progressBar.Value = 0;
        cancelButton.Visible = true;
        signalPercentLabel.Text = "00%";
        signalCodeLabel.Text = $"PKG {Program.ProductVersion} / RUNNING";
        cancellation = new CancellationTokenSource();
        try
        {
            var report = new Progress<InstallProgress>(item =>
            {
                statusLabel.Text = item.Message;
                if (item.Percent is int value)
                {
                    int clamped = Math.Clamp(value, 0, 100);
                    progressBar.Value = clamped;
                    signalPercentLabel.Text = $"{clamped:00}%";
                }
            });
            await InstallerEngine.InstallAsync(installPath.Text.Trim(), libraryPath.Text.Trim(), languagePicker.SelectedValue?.ToString() ?? "en", report, cancellation.Token);
            progressBar.Value = 100;
            signalPercentLabel.Text = "100%";
            signalCodeLabel.Text = $"PKG {Program.ProductVersion} / READY";
            statusLabel.Text = "READY / CineForge Desktop and the required Wan model pack are installed and verified.";
            cancelButton.Visible = false;
            installButton.Text = "LAUNCH CINEFORGE DESKTOP  →";
            installButton.Enabled = true;
            installButton.Click -= InstallClicked;
            installButton.Click += (_, _) => { InstallerEngine.Launch(installPath.Text.Trim(), libraryPath.Text.Trim()); Close(); };
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "PAUSED / Run setup again to resume from the saved partial files.";
            ResetForRetry();
        }
        catch (Exception ex)
        {
            statusLabel.Text = "ATTENTION / Existing partial model downloads were preserved.";
            ResetForRetry();
            MessageBox.Show(ex.Message, "CineForge Desktop setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetForRetry()
    {
        cancelButton.Visible = false;
        signalCodeLabel.Text = $"PKG {Program.ProductVersion} / READY";
        signalPercentLabel.Text = $"{progressBar.Value:00}%";
        installButton.Text = "INSTALL + DOWNLOAD  →";
        installButton.Enabled = true;
        installPath.Enabled = true;
        libraryPath.Enabled = true;
        languagePicker.Enabled = true;
    }
}

internal static class InstallerPalette
{
    internal static readonly Color BrandGreen = Color.FromArgb(228, 255, 26);
    internal static readonly Color Black = Color.FromArgb(2, 3, 0);
    internal static readonly Color Carbon = Color.FromArgb(36, 36, 36);
    internal static readonly Color Alabaster = Color.FromArgb(224, 224, 224);
    internal static readonly Color SoftText = Color.FromArgb(185, 185, 185);
    internal static readonly Color Line = Color.FromArgb(82, 82, 82);
    internal static readonly Color DarkText = Color.FromArgb(8, 8, 8);
}

internal static class BrandAssets
{
    internal static Bitmap IconMarkAcid => LoadBitmap("CineForge.Brand.icon-mark-acid-512.png");
    internal static Bitmap WordmarkAlabaster => LoadBitmap("CineForge.Brand.wordmark-alabaster-512.png");
    internal static Bitmap BuildVersionBadge(string version)
        => LoadBitmap("CineForge.Brand.version-badge-filled-acid-v5.1.png");

    internal static Bitmap CreateNoiseTexture(int width, int height)
    {
        var bmp = new Bitmap(width, height);
        var rng = new Random(17);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int shade = 255 - rng.Next(0, 16);
                int alpha = rng.Next(0, 12);
                bmp.SetPixel(x, y, Color.FromArgb(alpha, shade, shade, shade));
            }
        }
        return bmp;
    }

    private static Bitmap LoadBitmap(string resourceName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded brand asset: {resourceName}");
        return new Bitmap(stream);
    }

    private static Bitmap PrepareTransparent(Bitmap source)
    {
        var bmp = new Bitmap(source.Width, source.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                if (pixel.R > 245 && pixel.G > 245 && pixel.B > 245)
                    bmp.SetPixel(x, y, Color.Transparent);
                else
                    bmp.SetPixel(x, y, pixel);
            }
        }
        return bmp;
    }

    private static Bitmap TrimTransparentBounds(Bitmap source)
    {
        int left = source.Width;
        int top = source.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                if (source.GetPixel(x, y).A == 0) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left || bottom < top) return source;
        var rect = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        var trimmed = new Bitmap(rect.Width, rect.Height);
        using var g = Graphics.FromImage(trimmed);
        g.DrawImage(source, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        return trimmed;
    }
}

internal sealed class FrameMarks : Control
{
    public FrameMarks()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(210, 210, 210), 1);
        DrawCorner(e.Graphics, pen, 0, 0, false, false);
        DrawCorner(e.Graphics, pen, Width - 18, 0, true, false);
        DrawCorner(e.Graphics, pen, 0, Height - 18, false, true);
        DrawCorner(e.Graphics, pen, Width - 18, Height - 18, true, true);
    }

    private static void DrawCorner(Graphics g, Pen pen, int x, int y, bool right, bool bottom)
    {
        int h = 12;
        int v = 14;
        if (!right && !bottom)
        {
            g.DrawLine(pen, x, y + v, x, y);
            g.DrawLine(pen, x, y, x + h, y);
        }
        else if (right && !bottom)
        {
            g.DrawLine(pen, x + h, y + v, x + h, y);
            g.DrawLine(pen, x, y, x + h, y);
        }
        else if (!right && bottom)
        {
            g.DrawLine(pen, x, y, x, y + v);
            g.DrawLine(pen, x, y + v, x + h, y + v);
        }
        else
        {
            g.DrawLine(pen, x + h, y, x + h, y + v);
            g.DrawLine(pen, x, y + v, x + h, y + v);
        }
    }
}

internal sealed class RuleLine : Control
{
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool DotAlignedRight { get; set; }

    public RuleLine()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(110, 110, 110), 1f);
        int y = Height / 2;
        e.Graphics.DrawLine(pen, 0, y, Width - 12, y);
        if (DotAlignedRight) e.Graphics.DrawEllipse(pen, Width - 10, y - 2, 4, 4);
    }
}

internal sealed class NotchedPanel : Panel
{
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string PanelTitle { get; set; } = "";
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string PanelCode { get; set; } = "";

    public NotchedPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(34, 34, 34);
        Padding = new Padding(18, 20, 18, 18);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        using var path = BuildPath(ClientRectangle, 14);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = BuildPath(rect, 14);
        using var fill = new SolidBrush(Color.FromArgb(34, 34, 34));
        using var border = new Pen(Color.FromArgb(96, 96, 96), 1.1f);
        e.Graphics.FillPath(fill, path);
        DrawOpenCutBorder(e.Graphics, border, rect, 14);

        using var micro = new SolidBrush(InstallerPalette.BrandGreen);
        e.Graphics.FillRectangle(micro, 16, 16, 34, 3);

        using var titleBrush = new SolidBrush(InstallerPalette.BrandGreen);
        e.Graphics.DrawString(PanelTitle, new Font("Consolas", 9, FontStyle.Bold), titleBrush, new PointF(18, 24));

        if (!string.IsNullOrWhiteSpace(PanelCode))
        {
            var codeSize = e.Graphics.MeasureString(PanelCode, new Font("Consolas", 8.5f));
            var codeRect = new RectangleF(Width - codeSize.Width - 24, 18, codeSize.Width + 10, 18);
            using var bg = new SolidBrush(Color.FromArgb(12, 12, 12));
            e.Graphics.FillRectangle(bg, codeRect);
            e.Graphics.DrawString(PanelCode, new Font("Consolas", 8.5f), Brushes.Gainsboro, codeRect.X + 4, codeRect.Y + 2);
        }
    }

    private static void DrawOpenCutBorder(Graphics graphics, Pen pen, Rectangle rect, int cut)
    {
        float left = rect.Left + 0.5f;
        float top = rect.Top + 0.5f;
        float right = rect.Right - 0.5f;
        float bottom = rect.Bottom - 0.5f;

        graphics.DrawLine(pen, left + cut, top, right, top);
        graphics.DrawLine(pen, right, top, right, bottom - cut);
        graphics.DrawLine(pen, right - cut, bottom, left, bottom);
        graphics.DrawLine(pen, left, bottom, left, top + cut);
    }

    internal static GraphicsPath BuildPath(Rectangle rect, int cut)
    {
        var path = new GraphicsPath();
        path.AddPolygon([
            new Point(rect.Left + cut, rect.Top),
            new Point(rect.Right, rect.Top),
            new Point(rect.Right, rect.Bottom - cut),
            new Point(rect.Right - cut, rect.Bottom),
            new Point(rect.Left, rect.Bottom),
            new Point(rect.Left, rect.Top + cut)
        ]);
        path.CloseFigure();
        return path;
    }
}

internal sealed class AngledButton : Button
{
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Color FillColor { get; set; } = InstallerPalette.Carbon;

    public AngledButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI", 10, FontStyle.Bold);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = BuildPath(ClientRectangle);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = BuildPath(rect);
        using var fill = new SolidBrush(Enabled ? FillColor : Color.FromArgb(58, 58, 58));
        using var border = new Pen(Enabled ? InstallerPalette.Line : Color.FromArgb(78, 78, 78), 1f);
        pevent.Graphics.FillPath(fill, path);
        pevent.Graphics.DrawPath(border, path);

        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
        TextRenderer.DrawText(pevent.Graphics, Text, Font, rect, ForeColor, flags);
    }

    private static GraphicsPath BuildPath(Rectangle rect)
    {
        int cut = Math.Min(16, Math.Max(8, rect.Height / 3));
        var path = new GraphicsPath();
        path.AddPolygon([
            new Point(rect.Left, rect.Top),
            new Point(rect.Right - cut, rect.Top),
            new Point(rect.Right, rect.Top + cut),
            new Point(rect.Right, rect.Bottom),
            new Point(rect.Left + cut, rect.Bottom),
            new Point(rect.Left, rect.Bottom - cut)
        ]);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SegmentedProgressBar : Control
{
    private int value;
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal int Value
    {
        get => value;
        set { this.value = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    public SegmentedProgressBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        BackColor = Color.Transparent;
        ForeColor = InstallerPalette.BrandGreen;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        int segments = 52;
        int gap = 3;
        int width = Math.Max(4, (Width - ((segments - 1) * gap)) / segments);
        int lit = (int)Math.Round((Value / 100d) * segments);
        for (int i = 0; i < segments; i++)
        {
            int x = i * (width + gap);
            using var brush = new SolidBrush(i < lit ? InstallerPalette.BrandGreen : Color.FromArgb(72, 72, 72));
            e.Graphics.FillRectangle(brush, x, 0, width, Height - 1);
        }
        using var border = new Pen(Color.FromArgb(58, 58, 58));
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }
}

internal static class NativeMethods
{
    internal const int WM_NCLBUTTONDOWN = 0xA1;
    internal const int HTCAPTION = 0x2;

    [DllImport("user32.dll")]
    internal static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}

internal sealed record InstallProgress(string Message, int? Percent = null, long BytesReceived = 0, long TotalBytes = 0);
internal sealed record ModelFile(string Name, long Bytes, string Sha256);
internal sealed record LanguageOption(string Id, string Label);

internal static class InstallerEngine
{
    private static readonly string RuntimeFileName = RuntimeMetadata("CineForgeRuntimeFileName");
    private static readonly string RuntimeUrl = RuntimeMetadata("CineForgeRuntimeUrl");
    private static readonly long RuntimeBytes = long.Parse(RuntimeMetadata("CineForgeRuntimeBytes"));
    private static readonly string RuntimeSha256 = RuntimeMetadata("CineForgeRuntimeSha256");
    private const string ModelRepository = "https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models";
    private const string ModelRevision = "493b7c8ff0a451b6b4c049afb3e6396dbfa1c688";
    private const string PackFolder = "CineForge-Wan-2.2-I2V-A14B-FP8";
    private const long SafetyMargin = 5L * 1024 * 1024 * 1024;
    private static readonly HttpClient Http = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly ModelFile[] ModelFiles = [
        new("components/wan2.2_i2v_high_noise_14B_fp8_scaled.safetensors", 14294742832, "6122e79d55e0f235698d11d657f3b196c5273c830da00b2b013c5a048d5e6a42"),
        new("components/wan2.2_i2v_low_noise_14B_fp8_scaled.safetensors", 14294742832, "5471a457b6ac404202a5fbe6c11595a3d5641fc766b00f38763f72303fffc21e"),
        new("components/umt5_xxl_fp8_e4m3fn_scaled.safetensors", 6735906897, "c3355d30191f1f066b26d93fba017ae9809dce6c627dda5f6a66eaa651204f68"),
        new("components/wan_2.1_vae.safetensors", 253815318, "2fc39d31359a4b0a64f55876d8ff7fa8d780956ae2cb13463b0223e15148976b"),
        new("support/scheduler/scheduler_config.json", 820, "40624e058848feddf4bc2da5e8a232668b9a6f4a4939365810bebd9ce0166578"),
        new("support/text_encoder/config.json", 855, "a2bcb24699f6c009a2427432bdd483ef8b2b42a712abc9503759cdc77d171f07"),
        new("support/tokenizer/special_tokens_map.json", 7079, "456b58fd240a06c743a7c2cf8008bec501240d68ebd1fc4018ea569505fea270"),
        new("support/tokenizer/spiece.model", 4548313, "e3909a67b780650b35cf529ac782ad2b6b26e6d1f849d3fbb6a872905f452458"),
        new("support/tokenizer/tokenizer_config.json", 61758, "1d8d2a216bf8e70ac15b7ddcea566c4dd0433c024b39a58ca5e4c66bd78defbd"),
        new("support/tokenizer/tokenizer.json", 16837459, "20a46ac256746594ed7e1e3ef733b83fbc5a6f0922aa7480eda961743de080ef"),
        new("support/transformer/config.json", 495, "6809423c4a92f886feded9f85f55c667d73a2332d5e011fab4172f3448dd5666"),
        new("support/transformer_2/config.json", 495, "6809423c4a92f886feded9f85f55c667d73a2332d5e011fab4172f3448dd5666"),
        new("support/vae/config.json", 724, "47e8bcf55e93e9c182e1962a8c7a0650faeb34ea0f66826d6f8aaa9f73e08ec9")
    ];

    private static string RuntimeMetadata(string key) => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(attribute => attribute.Key == key).Value
        ?? throw new InvalidOperationException($"Installer runtime metadata is missing: {key}");

    internal static async Task InstallAsync(string installRoot, string dataRoot, string language, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        installRoot = NormalizeInstallTarget(installRoot);
        dataRoot = NormalizeLibraryTarget(dataRoot);
        EnsureSeparateRoots(installRoot, dataRoot);
        CreateLibrary(dataRoot, installRoot, language);
        string runtimePayload = await AcquireRuntimePayloadAsync(dataRoot, progress, cancellationToken);
        InstallApplication(installRoot, runtimePayload, progress);
        await DownloadModelPackAsync(dataRoot, progress, cancellationToken);
        CreateShortcuts(installRoot);
        RegisterUninstaller(installRoot);
        WriteInstallPointer(installRoot, dataRoot);
    }

    private static void InstallApplication(string installRoot, string payloadPath, IProgress<InstallProgress>? progress)
    {
        string parent = Path.GetDirectoryName(installRoot)!;
        if (Directory.Exists(installRoot)) RequireInstallMarker(installRoot);
        Directory.CreateDirectory(parent);
        string staging = installRoot + $".installing-{Guid.NewGuid():N}";
        string backup = installRoot + ".backup";
        try
        {
            progress?.Report(new("Extracting CineForge application files…", 1));
            Directory.CreateDirectory(staging);
            using Stream payload = new FileStream(payloadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(payload, ZipArchiveMode.Read);
            archive.ExtractToDirectory(staging, overwriteFiles: true);
            if (!File.Exists(Path.Combine(staging, "CineForge.exe"))) throw new InvalidDataException("The installed CineForge executable is missing.");
            File.WriteAllText(Path.Combine(staging, ".cineforge-install"), Program.ProductVersion);
            StopInstalledApp(installRoot);
            if (Directory.Exists(backup))
            {
                RequireInstallMarker(backup);
                Directory.Delete(backup, true);
            }
            if (Directory.Exists(installRoot))
            {
                Directory.Move(installRoot, backup);
            }
            Directory.Move(staging, installRoot);
            File.Copy(Environment.ProcessPath!, Path.Combine(installRoot, "CineForge Desktop Setup.exe"), true);
            if (Directory.Exists(backup))
            {
                RequireInstallMarker(backup);
                Directory.Delete(backup, true);
            }
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (!Directory.Exists(installRoot) && Directory.Exists(backup)) Directory.Move(backup, installRoot);
            throw;
        }
    }

    private static async Task<string> AcquireRuntimePayloadAsync(string dataRoot, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        string downloads = Path.Combine(dataRoot, "cache", "downloads");
        Directory.CreateDirectory(downloads);
        string destination = Path.Combine(downloads, RuntimeFileName);
        if (File.Exists(destination) && new FileInfo(destination).Length == RuntimeBytes && await HashMatchesAsync(destination, RuntimeSha256, cancellationToken))
        {
            progress?.Report(new("CineForge native runtime verified.", 5, RuntimeBytes, RuntimeBytes));
            return destination;
        }
        if (File.Exists(destination)) File.Delete(destination);
        string partial = destination + ".partial";
        EnsureDiskSpace(dataRoot, RuntimeBytes - (File.Exists(partial) ? new FileInfo(partial).Length : 0) + SafetyMargin);
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                long offset = File.Exists(partial) ? new FileInfo(partial).Length : 0;
                if (offset > RuntimeBytes) { File.Delete(partial); offset = 0; }
                if (offset == RuntimeBytes)
                {
                    progress?.Report(new("Verifying completed CineForge runtime download…", 5, RuntimeBytes, RuntimeBytes));
                    if (await HashMatchesAsync(partial, RuntimeSha256, cancellationToken))
                    {
                        File.Move(partial, destination, true);
                        return destination;
                    }
                    File.Delete(partial);
                    offset = 0;
                }
                using var request = new HttpRequestMessage(HttpMethod.Get, RuntimeUrl);
                if (offset > 0) request.Headers.Range = new RangeHeaderValue(offset, null);
                using HttpResponseMessage response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && offset > 0)
                {
                    File.Delete(partial);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                bool append = offset > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                if (!append) offset = 0;
                await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = new FileStream(partial, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 1024, true);
                byte[] buffer = new byte[1024 * 1024];
                long current = offset;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    current += read;
                    progress?.Report(new($"Downloading CineForge native runtime — {FormatBytes(current)} / {FormatBytes(RuntimeBytes)}", 1 + (int)Math.Min(3, current * 4 / RuntimeBytes), current, RuntimeBytes));
                }
                await target.FlushAsync(cancellationToken);
                if (new FileInfo(partial).Length != RuntimeBytes) throw new InvalidDataException("The CineForge runtime downloaded with an unexpected size.");
                progress?.Report(new("Verifying CineForge native runtime…", 5, RuntimeBytes, RuntimeBytes));
                if (!await HashMatchesAsync(partial, RuntimeSha256, cancellationToken))
                {
                    File.Delete(partial);
                    throw new InvalidDataException("The CineForge runtime failed SHA-256 verification and will be downloaded again.");
                }
                File.Move(partial, destination, true);
                return destination;
            }
            catch when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }
        throw new IOException("CineForge could not finish downloading its native runtime. Run setup again to resume.");
    }

    private static void CreateLibrary(string dataRoot, string installRoot, string requestedLanguage)
    {
        string language = requestedLanguage is "ko" or "ja" ? requestedLanguage : "en";
        foreach (string folder in new[] { "inputs", "outputs", "projects", "models", "cache", "logs", "temp" })
            Directory.CreateDirectory(Path.Combine(dataRoot, folder));
        var config = new
        {
            inference_backend = "native",
            model_roots = new[] { Path.Combine(dataRoot, "models") },
            model_cache_root = Path.Combine(dataRoot, "cache"),
            input_root = Path.Combine(dataRoot, "inputs"),
            output_root = Path.Combine(dataRoot, "outputs"),
            language
        };
        File.WriteAllText(Path.Combine(dataRoot, "config.json"), JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        Directory.CreateDirectory(Program.PointerRoot);
        File.WriteAllText(Program.PointerPath, JsonSerializer.Serialize(new { install_root = installRoot, data_root = dataRoot, version = Program.ProductVersion, language }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task DownloadModelPackAsync(string dataRoot, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        string packRoot = Path.Combine(dataRoot, "models", PackFolder);
        Directory.CreateDirectory(packRoot);
        long total = ModelFiles.Sum(file => file.Bytes);
        long verified = 0;
        long missing = 0;
        foreach (ModelFile file in ModelFiles)
        {
            string destination = Path.Combine(packRoot, file.Name.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(destination) && new FileInfo(destination).Length == file.Bytes && await HashMatchesAsync(destination, file.Sha256, cancellationToken))
                verified += file.Bytes;
            else
                missing += Math.Max(0, file.Bytes - (File.Exists(destination + ".partial") ? new FileInfo(destination + ".partial").Length : 0));
        }
        EnsureDiskSpace(dataRoot, missing + SafetyMargin);
        foreach (ModelFile file in ModelFiles)
        {
            string destination = Path.Combine(packRoot, file.Name.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(destination) && new FileInfo(destination).Length == file.Bytes && await HashMatchesAsync(destination, file.Sha256, cancellationToken))
            {
                progress?.Report(new($"Verified {file.Name}", 5 + (int)(verified * 90 / total), verified, total));
                continue;
            }
            if (File.Exists(destination)) File.Delete(destination);
            await DownloadFileAsync(file, destination, verified, total, progress, cancellationToken);
            verified += file.Bytes;
        }
        await DownloadMetadataAsync(packRoot, "cineforge-model.json", cancellationToken);
        await DownloadMetadataAsync(packRoot, "CHECKSUMS.sha256", cancellationToken);
        File.WriteAllText(Path.Combine(packRoot, ".cineforge-pack-complete"), $"{DateTimeOffset.UtcNow:O}\n{ModelRevision}\n");
        progress?.Report(new("Wan model pack downloaded and SHA-256 verified.", 96, total, total));
    }

    private static async Task DownloadFileAsync(ModelFile file, string destination, long completedBefore, long total, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string partial = destination + ".partial";
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                long offset = File.Exists(partial) ? new FileInfo(partial).Length : 0;
                if (offset > file.Bytes) { File.Delete(partial); offset = 0; }
                if (offset == file.Bytes)
                {
                    progress?.Report(new($"Verifying completed {file.Name} download…", 5 + (int)((completedBefore + file.Bytes) * 90 / total), completedBefore + file.Bytes, total));
                    if (await HashMatchesAsync(partial, file.Sha256, cancellationToken))
                    {
                        File.Move(partial, destination, true);
                        return;
                    }
                    File.Delete(partial);
                    offset = 0;
                }
                string remotePath = string.Join('/', file.Name.Split('/').Select(Uri.EscapeDataString));
                string url = $"{ModelRepository}/resolve/{ModelRevision}/{remotePath}?download=true";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (offset > 0) request.Headers.Range = new RangeHeaderValue(offset, null);
                using HttpResponseMessage response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && offset > 0)
                {
                    File.Delete(partial);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                bool append = offset > 0 && response.StatusCode == HttpStatusCode.PartialContent;
                if (!append) offset = 0;
                await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var target = new FileStream(partial, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 1024, true);
                byte[] buffer = new byte[1024 * 1024];
                long current = offset;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    current += read;
                    long aggregate = completedBefore + current;
                    int percent = 5 + (int)Math.Min(89, aggregate * 90 / total);
                    progress?.Report(new($"Downloading Wan models — {file.Name}  {FormatBytes(current)} / {FormatBytes(file.Bytes)}", percent, aggregate, total));
                }
                await target.FlushAsync(cancellationToken);
                if (new FileInfo(partial).Length != file.Bytes) throw new InvalidDataException($"{file.Name} downloaded with an unexpected size.");
                progress?.Report(new($"Verifying {file.Name}…", 5 + (int)((completedBefore + file.Bytes) * 90 / total), completedBefore + file.Bytes, total));
                if (!await HashMatchesAsync(partial, file.Sha256, cancellationToken))
                {
                    File.Delete(partial);
                    throw new InvalidDataException($"{file.Name} failed SHA-256 verification and will be downloaded again.");
                }
                File.Move(partial, destination, true);
                return;
            }
            catch when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }
        throw new IOException($"CineForge could not finish downloading {file.Name}. Run setup again to resume.");
    }

    private static async Task<bool> HashMatchesAsync(string path, string expected, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task DownloadMetadataAsync(string packRoot, string name, CancellationToken cancellationToken)
    {
        string url = $"{ModelRepository}/resolve/{ModelRevision}/{name}?download=true";
        byte[] content = await Http.GetByteArrayAsync(url, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(packRoot, name), content, cancellationToken);
    }

    private static void EnsureDiskSpace(string target, long required)
    {
        string root = Path.GetPathRoot(Path.GetFullPath(target)) ?? throw new InvalidOperationException("The CineForge Library drive could not be resolved.");
        var drive = new DriveInfo(root);
        if (drive.AvailableFreeSpace < required)
            throw new IOException($"The selected CineForge Library drive needs at least {FormatBytes(required)} free. It currently has {FormatBytes(drive.AvailableFreeSpace)} available.");
    }

    private static string FormatBytes(long bytes) => $"{bytes / 1024d / 1024d / 1024d:0.0} GB";

    internal static void Uninstall(string installRoot)
    {
        installRoot = NormalizeInstallTarget(installRoot);
        RequireInstallMarker(installRoot);
        StopInstalledApp(installRoot);
        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CineForge Desktop.lnk"));
        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "CineForge Desktop.lnk"));
        Registry.CurrentUser.DeleteSubKeyTree(Program.UninstallKey, throwOnMissingSubKey: false);
        if (Directory.Exists(installRoot)) Directory.Delete(installRoot, true);
    }

    private static void StopInstalledApp(string installRoot)
    {
        foreach (var process in Process.GetProcessesByName("CineForge"))
        {
            try
            {
                string path = process.MainModule?.FileName ?? "";
                if (path.StartsWith(installRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch { }
        }
    }

    internal static void Launch(string installRoot, string dataRoot)
    {
        string executable = Path.Combine(installRoot, "CineForge.exe");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = installRoot };
        start.Environment["CINEFORGE_DATA_ROOT"] = dataRoot;
        Process.Start(start);
    }

    private static void RegisterUninstaller(string installRoot)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(Program.UninstallKey, true);
        string setup = Path.Combine(installRoot, "CineForge Desktop Setup.exe");
        key.SetValue("DisplayName", Program.ProductName);
        key.SetValue("DisplayVersion", Program.ProductVersion);
        key.SetValue("Publisher", Program.Publisher);
        key.SetValue("DisplayIcon", $"\"{Path.Combine(installRoot, "CineForge.exe")}\"");
        key.SetValue("InstallLocation", installRoot);
        key.SetValue("UninstallString", $"\"{setup}\" --uninstall");
        key.SetValue("QuietUninstallString", $"\"{setup}\" --uninstall --silent");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        long bytes = Directory.EnumerateFiles(installRoot, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
        key.SetValue("EstimatedSize", (int)Math.Min(int.MaxValue, bytes / 1024), RegistryValueKind.DWord);
    }

    private static void CreateShortcuts(string installRoot)
    {
        string target = Path.Combine(installRoot, "CineForge.exe");
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CineForge Desktop.lnk"), target, installRoot);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "CineForge Desktop.lnk"), target, installRoot);
    }

    private static void CreateShortcut(string path, string target, string installRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Type shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows shortcut support is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(path);
        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = installRoot;
        shortcut.IconLocation = target;
        shortcut.Description = "CineForge Desktop local Wan video generator";
        shortcut.Save();
        Marshal.FinalReleaseComObject(shortcut);
        Marshal.FinalReleaseComObject(shell);
    }

    private static void WriteInstallPointer(string installRoot, string dataRoot)
    {
        Directory.CreateDirectory(Program.PointerRoot);
        File.WriteAllText(Program.PointerPath, JsonSerializer.Serialize(new { install_root = installRoot, data_root = dataRoot, version = Program.ProductVersion }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string NormalizeInstallTarget(string path)
    {
        string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (!Path.GetFileName(full).Equals("CineForge", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The application folder must end in 'CineForge'. Use Browse to select its parent folder.");
        RejectBroadPath(full);
        return full;
    }

    private static string NormalizeLibraryTarget(string path)
    {
        string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (!Path.GetFileName(full).Equals("CineForge Library", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The library folder must end in 'CineForge Library'. Use Browse to select its parent folder.");
        RejectBroadPath(full);
        return full;
    }

    private static void RejectBroadPath(string full)
    {
        string root = Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar) ?? "";
        if (full.Equals(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("A drive root cannot be used as a CineForge folder.");
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd(Path.DirectorySeparatorChar);
        if (full.Equals(windows, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The Windows folder cannot be used as a CineForge folder.");
    }

    private static void EnsureSeparateRoots(string installRoot, string dataRoot)
    {
        string app = installRoot + Path.DirectorySeparatorChar;
        string data = dataRoot + Path.DirectorySeparatorChar;
        if (installRoot.Equals(dataRoot, StringComparison.OrdinalIgnoreCase) || app.StartsWith(data, StringComparison.OrdinalIgnoreCase) || data.StartsWith(app, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The application and CineForge Library must be separate folders, and neither may be inside the other.");
    }

    private static void RequireInstallMarker(string installRoot)
    {
        if (!File.Exists(Path.Combine(installRoot, ".cineforge-install")))
            throw new InvalidOperationException("CineForge refused to replace or remove a folder that is not marked as its own installation.");
    }

    private static void DeleteShortcut(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
