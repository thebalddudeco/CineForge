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
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.4.0";
    internal static readonly string ProgramsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
    internal static readonly string DefaultInstallRoot = Path.Combine(ProgramsRoot, InstallFolderName);
    internal static readonly string DefaultDataRoot = DefaultLibraryRoot();
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
            InstallerEngine.InstallAsync(DefaultInstallRoot, DefaultDataRoot, null, CancellationToken.None).GetAwaiter().GetResult();
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
    private readonly ProgressBar progressBar;
    private readonly Button installButton;
    private readonly Button cancelButton;
    private readonly TextBox installPath;
    private readonly TextBox libraryPath;
    private CancellationTokenSource? cancellation;

    public InstallerForm()
    {
        Text = $"CineForge Desktop {Program.ProductVersion} Setup";
        ClientSize = new Size(760, 590);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(9, 9, 8);
        ForeColor = Color.FromArgb(233, 229, 220);
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);

        var accent = new Panel { BackColor = Color.FromArgb(215, 255, 69), Location = new Point(0, 0), Size = new Size(8, 590) };
        var eyebrow = new Label { Text = "CINEFORGE DESKTOP / LOCAL WAN VIDEO SYSTEM", AutoSize = true, ForeColor = Color.FromArgb(215, 255, 69), Font = new Font("Segoe UI Semibold", 9), Location = new Point(54, 35) };
        var title = new Label { Text = "Install CineForge Desktop", AutoSize = true, Font = new Font("Segoe UI", 28, FontStyle.Bold), Location = new Point(49, 63) };
        var subtitle = new Label { Text = "Application and library locations stay completely separate from Shadowframe.", AutoSize = true, ForeColor = Color.FromArgb(182, 176, 162), Font = new Font("Segoe UI", 10), Location = new Point(54, 118) };

        var appLabel = FieldLabel("APPLICATION FOLDER", 165);
        installPath = PathBox(190, Program.DefaultInstallRoot);
        var appBrowse = BrowseButton(190, () => BrowseFor(installPath, "Select a parent folder for CineForge", "CineForge"));

        var libraryLabel = FieldLabel("CINEFORGE LIBRARY", 255);
        libraryPath = PathBox(280, Program.DefaultDataRoot);
        var libraryBrowse = BrowseButton(280, () => BrowseFor(libraryPath, "Select a parent folder for the CineForge Library", "CineForge Library"));
        var libraryNote = new Label {
            Text = "Creates the separate CineForge Library for inputs, outputs, projects, models, cache, logs, and temp.\nSetup downloads a ~2.0 GB native runtime, then approximately 35.6 GB of required Wan models.",
            AutoSize = true, ForeColor = Color.FromArgb(125, 123, 117), Font = new Font("Segoe UI", 9), Location = new Point(55, 320)
        };

        progressBar = new ProgressBar { Location = new Point(55, 405), Size = new Size(650, 10), Style = ProgressBarStyle.Continuous, Visible = false };
        statusLabel = new Label { Text = "Ready to install", AutoEllipsis = true, Size = new Size(650, 38), ForeColor = Color.FromArgb(145, 143, 137), Location = new Point(55, 430) };
        cancelButton = new Button { Text = "Pause", Size = new Size(110, 44), Location = new Point(375, 505), BackColor = Color.FromArgb(24, 24, 21), ForeColor = Color.FromArgb(233, 229, 220), FlatStyle = FlatStyle.Flat, Visible = false };
        installButton = new Button { Text = "Install + Download  →", Size = new Size(210, 44), Location = new Point(495, 505), BackColor = Color.FromArgb(215, 255, 69), ForeColor = Color.FromArgb(9, 9, 8), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10) };
        installButton.FlatAppearance.BorderSize = 0;
        cancelButton.Click += (_, _) => cancellation?.Cancel();
        installButton.Click += InstallClicked;
        Controls.AddRange([accent, eyebrow, title, subtitle, appLabel, installPath, appBrowse, libraryLabel, libraryPath, libraryBrowse, libraryNote, progressBar, statusLabel, cancelButton, installButton]);
    }

    private static Label FieldLabel(string text, int y) => new() { Text = text, AutoSize = true, ForeColor = Color.FromArgb(215, 255, 69), Font = new Font("Consolas", 8), Location = new Point(55, y) };
    private static TextBox PathBox(int y, string value) => new() { Text = value, Size = new Size(550, 27), BackColor = Color.FromArgb(17, 17, 15), ForeColor = Color.FromArgb(233, 229, 220), BorderStyle = BorderStyle.FixedSingle, Location = new Point(55, y) };
    private static Button BrowseButton(int y, Action action)
    {
        var button = new Button { Text = "Browse…", Size = new Size(90, 28), Location = new Point(615, y - 1), FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(233, 229, 220), BackColor = Color.FromArgb(24, 24, 21) };
        button.Click += (_, _) => action();
        return button;
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
        progressBar.Visible = true;
        progressBar.Value = 0;
        cancelButton.Visible = true;
        installButton.Location = new Point(495, 505);
        cancellation = new CancellationTokenSource();
        try
        {
            var report = new Progress<InstallProgress>(item =>
            {
                statusLabel.Text = item.Message;
                if (item.Percent is int value) progressBar.Value = Math.Clamp(value, 0, 100);
            });
            await InstallerEngine.InstallAsync(installPath.Text.Trim(), libraryPath.Text.Trim(), report, cancellation.Token);
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
    }
}

internal sealed record InstallProgress(string Message, int? Percent = null, long BytesReceived = 0, long TotalBytes = 0);
internal sealed record ModelFile(string Name, long Bytes, string Sha256);

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

    internal static async Task InstallAsync(string installRoot, string dataRoot, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        installRoot = NormalizeInstallTarget(installRoot);
        dataRoot = NormalizeLibraryTarget(dataRoot);
        EnsureSeparateRoots(installRoot, dataRoot);
        CreateLibrary(dataRoot, installRoot);
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

    private static void CreateLibrary(string dataRoot, string installRoot)
    {
        foreach (string folder in new[] { "inputs", "outputs", "projects", "models", "cache", "logs", "temp" })
            Directory.CreateDirectory(Path.Combine(dataRoot, folder));
        var config = new
        {
            host = "127.0.0.1",
            port = 7331,
            inference_backend = "native",
            model_roots = new[] { Path.Combine(dataRoot, "models") },
            model_cache_root = Path.Combine(dataRoot, "cache"),
            input_root = Path.Combine(dataRoot, "inputs"),
            output_root = Path.Combine(dataRoot, "outputs")
        };
        File.WriteAllText(Path.Combine(dataRoot, "config.json"), JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        Directory.CreateDirectory(Program.PointerRoot);
        File.WriteAllText(Program.PointerPath, JsonSerializer.Serialize(new { install_root = installRoot, data_root = dataRoot, version = Program.ProductVersion }, new JsonSerializerOptions { WriteIndented = true }));
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
