using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
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
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.5.0";
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
    private readonly Button installButton;
    private readonly Button cancelButton;
    private readonly TextBox installPath;
    private readonly TextBox libraryPath;
    private readonly CineForgeSelector languagePicker;
    private CancellationTokenSource? cancellation;

    public InstallerForm()
    {
        Text = $"CineForge Desktop {Program.ProductVersion} Setup";
        ClientSize = new Size(880, 720);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = CineForgeTheme.Black;
        ForeColor = CineForgeTheme.Alabaster;
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        DoubleBuffered = true;
        Padding = new Padding(1);

        var surface = new InstallerSurface { Dock = DockStyle.Fill };
        var chrome = new Panel { BackColor = CineForgeTheme.Black, Location = new Point(1, 1), Size = new Size(878, 40) };
        var chromeMark = new PictureBox { Image = LoadEmbeddedBitmap("CineForge.Installer.Brand.icon-mark-acid-64.png"), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Location = new Point(14, 8), Size = new Size(18, 18) };
        var chromeTitle = new Label { Text = $"CINEFORGE DESKTOP  /  SETUP SYSTEM  /  {Program.ProductVersion}", AutoSize = true, ForeColor = CineForgeTheme.Alabaster, Font = CineForgeTheme.Mono(8), Location = new Point(44, 14) };
        var minimize = ChromeButton("—", 800, (_, _) => WindowState = FormWindowState.Minimized);
        var close = ChromeButton("×", 838, (_, _) => Close());
        chrome.Controls.AddRange([chromeMark, chromeTitle, minimize, close]);
        chrome.MouseDown += DragWindow;
        chromeTitle.MouseDown += DragWindow;

        var rail = new Panel { BackColor = CineForgeTheme.Line, Location = new Point(40, 58), Size = new Size(800, 1) };
        var railDot = new Panel { BackColor = CineForgeTheme.Chartreuse, Location = new Point(830, 55), Size = new Size(7, 7) };
        var badge = new PictureBox { Image = LoadEmbeddedBitmap("CineForge.Installer.Brand.version-badge-filled-acid-v0.5.0.png"), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Location = new Point(765, 76), Size = new Size(76, 82) };
        var eyebrow = new Label { Text = "■  CINEFORGE DESKTOP / LOCAL WAN VIDEO SYSTEM", AutoSize = true, ForeColor = CineForgeTheme.Chartreuse, Font = CineForgeTheme.Mono(8, FontStyle.Bold), Location = new Point(55, 84) };
        var title = new Label { Text = "Install CineForge Desktop", AutoSize = true, ForeColor = CineForgeTheme.Alabaster, Font = CineForgeTheme.Title(27), Location = new Point(50, 108) };
        var subtitle = new Label { Text = "Choose the application and private local-library locations. Existing verified files are reused.", AutoSize = true, ForeColor = CineForgeTheme.Muted, Font = CineForgeTheme.Body(10), Location = new Point(55, 154) };

        var setupPanel = new InstrumentPanel { DoubleFrame = true, Location = new Point(40, 188), Size = new Size(800, 338) };
        var panelCode = new Label { Text = "01 / INSTALLATION ROUTER", AutoSize = true, BackColor = Color.Transparent, ForeColor = CineForgeTheme.Chartreuse, Font = CineForgeTheme.Mono(8, FontStyle.Bold), Location = new Point(24, 25) };
        var panelState = new Label { Text = "LOCAL / DESKTOP", AutoSize = true, BackColor = Color.Transparent, ForeColor = CineForgeTheme.Muted, Font = CineForgeTheme.Mono(7), Location = new Point(676, 25) };

        var languageLabel = FieldLabel("LANGUAGE / 언어 / 言語", 57);
        languagePicker = new CineForgeSelector { Location = new Point(25, 78), Size = new Size(225, 29) };
        languagePicker.SetOptions([new LanguageOption("en", "EN  English"), new LanguageOption("ko", "한  한국어"), new LanguageOption("ja", "日  日本語")], Program.DefaultLanguage);

        var appLabel = FieldLabel("APPLICATION FOLDER", 119);
        installPath = PathBox(140, Program.DefaultInstallRoot);
        var appBrowse = BrowseButton(140, () => BrowseFor(installPath, "Select a parent folder for CineForge", "CineForge"));

        var libraryLabel = FieldLabel("CINEFORGE LIBRARY", 186);
        libraryPath = PathBox(207, Program.DefaultDataRoot);
        var libraryBrowse = BrowseButton(207, () => BrowseFor(libraryPath, "Select a parent folder for the CineForge Library", "CineForge Library"));
        var libraryNote = new Label {
            Text = "SEPARATE DATA VAULT  /  inputs · outputs · projects · models · cache · logs · temp\nSetup verifies existing components before downloading the ~2.0 GB runtime and 35.6 GB Wan pack.",
            AutoSize = true, BackColor = Color.Transparent, ForeColor = CineForgeTheme.Muted, Font = CineForgeTheme.Body(8), Location = new Point(25, 249)
        };
        var divider = new Panel { BackColor = CineForgeTheme.Line, Location = new Point(25, 295), Size = new Size(750, 1) };
        setupPanel.Controls.AddRange([panelCode, panelState, languageLabel, languagePicker, appLabel, installPath, appBrowse, libraryLabel, libraryPath, libraryBrowse, libraryNote, divider]);

        var telemetryPanel = new InstrumentPanel { Location = new Point(40, 542), Size = new Size(800, 96) };
        var telemetryLabel = new Label { Text = "■  INSTALLATION SIGNAL", AutoSize = true, BackColor = Color.Transparent, ForeColor = CineForgeTheme.Alabaster, Font = CineForgeTheme.Mono(8), Location = new Point(20, 17) };
        var telemetryCode = new Label { Text = "PKG.050 / READY", AutoSize = true, BackColor = Color.Transparent, ForeColor = CineForgeTheme.Muted, Font = CineForgeTheme.Mono(7), Location = new Point(680, 18) };
        progressBar = new SegmentedProgressBar { Location = new Point(20, 42), Size = new Size(760, 15), Visible = true, Value = 0 };
        statusLabel = new Label { Text = "READY / Awaiting installation command", AutoEllipsis = true, Size = new Size(760, 22), ForeColor = CineForgeTheme.Muted, Font = CineForgeTheme.Mono(8), Location = new Point(20, 65) };
        telemetryPanel.Controls.AddRange([telemetryLabel, telemetryCode, progressBar, statusLabel]);

        cancelButton = new CineForgeButton { Text = "PAUSE", Size = new Size(120, 44), Location = new Point(505, 657), Visible = false };
        installButton = new CineForgeButton { Text = "INSTALL + DOWNLOAD  →", Size = new Size(205, 44), Location = new Point(635, 657), Primary = true };
        var footer = new Label { Text = "LOCAL-FIRST / VERIFIED COMPONENTS / PRIVATE RUNTIME", AutoSize = true, ForeColor = CineForgeTheme.Muted, Font = CineForgeTheme.Mono(7), Location = new Point(40, 672) };
        var marks = new RegistrationMarks { Location = new Point(24, 72), Size = new Size(832, 574) };
        marks.Enabled = false;
        cancelButton.Click += (_, _) => cancellation?.Cancel();
        installButton.Click += InstallClicked;
        surface.Controls.AddRange([rail, railDot, marks, badge, eyebrow, title, subtitle, setupPanel, telemetryPanel, footer, cancelButton, installButton]);
        marks.SendToBack();
        Controls.AddRange([surface, chrome]);
        chrome.BringToFront();
    }

    private static Button ChromeButton(string text, int x, EventHandler action)
    {
        var button = new Button { Text = text, Location = new Point(x, 2), Size = new Size(38, 35), FlatStyle = FlatStyle.Flat, BackColor = CineForgeTheme.Black, ForeColor = CineForgeTheme.Alabaster, Font = CineForgeTheme.Control(12) };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = CineForgeTheme.Carbon;
        button.FlatAppearance.MouseDownBackColor = CineForgeTheme.Chartreuse;
        button.Click += action;
        return button;
    }

    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, 0xA1, 0x2, 0);
    }

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle, int message, int wParam, int lParam);

    private static Label FieldLabel(string text, int y) => new() { Text = text, AutoSize = true, BackColor = Color.Transparent, ForeColor = CineForgeTheme.Chartreuse, Font = CineForgeTheme.Mono(8, FontStyle.Bold), Location = new Point(25, y) };
    private static TextBox PathBox(int y, string value) => new() { Text = value, Size = new Size(635, 29), BackColor = CineForgeTheme.Black, ForeColor = CineForgeTheme.Alabaster, BorderStyle = BorderStyle.FixedSingle, Font = CineForgeTheme.Body(9), Location = new Point(25, y) };
    private static Button BrowseButton(int y, Action action)
    {
        var button = new CineForgeButton { Text = "BROWSE…", Size = new Size(105, 30), Location = new Point(670, y - 1) };
        button.Click += (_, _) => action();
        return button;
    }

    private static Bitmap LoadEmbeddedBitmap(string logicalName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Missing embedded brand asset: {logicalName}");
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
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
        progressBar.Visible = true;
        progressBar.Value = 0;
        cancelButton.Visible = true;
        installButton.Location = new Point(635, 657);
        cancellation = new CancellationTokenSource();
        try
        {
            var report = new Progress<InstallProgress>(item =>
            {
                statusLabel.Text = item.Message;
                if (item.Percent is int value) progressBar.Value = Math.Clamp(value, 0, 100);
            });
            await InstallerEngine.InstallAsync(installPath.Text.Trim(), libraryPath.Text.Trim(), languagePicker.SelectedValue, report, cancellation.Token);
            progressBar.Value = 100;
            statusLabel.Text = "CineForge Desktop and the required Wan model pack are installed and verified.";
            cancelButton.Visible = false;
            installButton.Text = "Launch CineForge Desktop";
            installButton.Enabled = true;
            installButton.Click -= InstallClicked;
            installButton.Click += (_, _) => { InstallerEngine.Launch(installPath.Text.Trim(), libraryPath.Text.Trim()); Close(); };
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Download paused. Run setup again to resume from the saved partial files.";
            ResetForRetry();
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Setup needs attention. Existing partial model downloads were preserved.";
            ResetForRetry();
            MessageBox.Show(ex.Message, "CineForge Desktop setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetForRetry()
    {
        cancelButton.Visible = false;
        installButton.Text = "Retry setup  →";
        installButton.Enabled = true;
        installPath.Enabled = true;
        libraryPath.Enabled = true;
        languagePicker.Enabled = true;
    }
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
        Directory.CreateDirectory(parent);
        string staging = installRoot + $".installing-{Guid.NewGuid():N}";
        string backup = installRoot + ".backup";
        // A fixed backup name lets a later setup run recover an upgrade that was
        // interrupted after the old application directory had been moved aside.
        if (!Directory.Exists(installRoot) && Directory.Exists(backup))
        {
            RequireInstallMarker(backup);
            Directory.Move(backup, installRoot);
        }
        bool replacingExistingInstall = Directory.Exists(installRoot);
        if (replacingExistingInstall) RequireInstallMarker(installRoot);
        bool stagedApplicationPromoted = false;
        try
        {
            progress?.Report(new(replacingExistingInstall
                ? "Preparing the in-place CineForge upgrade…"
                : "Extracting CineForge application files…", 1));
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
            stagedApplicationPromoted = true;
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
            if (Directory.Exists(backup))
            {
                RequireInstallMarker(backup);
                if (Directory.Exists(installRoot))
                {
                    RequireInstallMarker(installRoot);
                    Directory.Delete(installRoot, true);
                }
                Directory.Move(backup, installRoot);
            }
            else if (stagedApplicationPromoted && Directory.Exists(installRoot))
            {
                RequireInstallMarker(installRoot);
                Directory.Delete(installRoot, true);
            }
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
