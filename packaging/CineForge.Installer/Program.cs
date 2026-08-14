using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CineForge.Installer;

internal static class Program
{
    internal const string ProductName = "CineForge Local";
    internal const string ProductVersion = "0.2.0";
    internal const string Publisher = "The Bald Dude Co.";
    internal static readonly string ProgramsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
    internal static readonly string InstallRoot = Path.Combine(ProgramsRoot, ProductName);
    internal static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CineForge");
    internal const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CineForgeLocal";

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
            InstallerEngine.Install(progress: null);
            return;
        }
        Application.Run(new InstallerForm());
    }

    private static void LaunchUninstallWorker(bool silent)
    {
        string tempCopy = Path.Combine(Path.GetTempPath(), $"CineForge-Uninstall-{Guid.NewGuid():N}.exe");
        File.Copy(Environment.ProcessPath!, tempCopy, true);
        Process.Start(new ProcessStartInfo(tempCopy, $"--uninstall-worker{(silent ? " --silent" : "")}") { UseShellExecute = true });
    }

    private static void RunUninstallWorker(bool silent)
    {
        if (!silent)
        {
            var answer = MessageBox.Show(
                "Remove CineForge Local from this PC?\n\nYour projects and reference uploads will be preserved.",
                "Uninstall CineForge Local", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }
        try
        {
            InstallerEngine.Uninstall();
            if (!silent) MessageBox.Show("CineForge Local was removed. Your local projects were preserved.", "Uninstall complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    private readonly ProgressBar progress;
    private readonly Button installButton;

    public InstallerForm()
    {
        Text = $"CineForge Local {Program.ProductVersion} Setup";
        ClientSize = new Size(680, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(9, 9, 8);
        ForeColor = Color.FromArgb(233, 229, 220);
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);

        var accent = new Panel { BackColor = Color.FromArgb(215, 255, 69), Location = new Point(0, 0), Size = new Size(8, 430) };
        var eyebrow = new Label { Text = "LOCAL CINEMATIC STUDIO", AutoSize = true, ForeColor = Color.FromArgb(215, 255, 69), Font = new Font("Segoe UI Semibold", 9), Location = new Point(54, 45) };
        var title = new Label { Text = "Install CineForge", AutoSize = true, Font = new Font("Segoe UI", 30, FontStyle.Bold), Location = new Point(49, 78) };
        var subtitle = new Label { Text = "Build the sequence, not just the frame.", AutoSize = true, ForeColor = Color.FromArgb(182, 176, 162), Font = new Font("Georgia", 14, FontStyle.Italic), Location = new Point(54, 137) };
        var description = new Label {
            Text = "Installs the CineForge desktop application, Start Menu shortcut,\nand local model-discovery tools for this Windows account.",
            AutoSize = true, ForeColor = Color.FromArgb(145, 143, 137), Font = new Font("Segoe UI", 10), Location = new Point(55, 190)
        };
        var destination = new Label { Text = Program.InstallRoot, AutoEllipsis = true, Size = new Size(570, 20), ForeColor = Color.FromArgb(96, 95, 91), Font = new Font("Consolas", 8), Location = new Point(55, 250) };
        progress = new ProgressBar { Location = new Point(55, 290), Size = new Size(570, 8), Style = ProgressBarStyle.Continuous, Visible = false };
        statusLabel = new Label { Text = "Ready to install", AutoSize = true, ForeColor = Color.FromArgb(145, 143, 137), Location = new Point(55, 315) };
        installButton = new Button { Text = "Install CineForge  →", Size = new Size(190, 46), Location = new Point(435, 355), BackColor = Color.FromArgb(215, 255, 69), ForeColor = Color.FromArgb(9, 9, 8), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10) };
        installButton.FlatAppearance.BorderSize = 0;
        installButton.Click += InstallClicked;
        Controls.AddRange([accent, eyebrow, title, subtitle, description, destination, progress, statusLabel, installButton]);
    }

    private async void InstallClicked(object? sender, EventArgs e)
    {
        installButton.Enabled = false;
        progress.Visible = true;
        progress.Style = ProgressBarStyle.Marquee;
        statusLabel.Text = "Installing CineForge Local…";
        try
        {
            await Task.Run(() => InstallerEngine.Install(message => BeginInvoke(() => statusLabel.Text = message)));
            progress.Style = ProgressBarStyle.Blocks;
            progress.Value = 100;
            statusLabel.Text = "Installation complete";
            installButton.Text = "Launch CineForge";
            installButton.Enabled = true;
            installButton.Click -= InstallClicked;
            installButton.Click += (_, _) => { InstallerEngine.Launch(); Close(); };
        }
        catch (Exception ex)
        {
            progress.Visible = false;
            statusLabel.Text = "Installation failed";
            installButton.Enabled = true;
            MessageBox.Show(ex.Message, "CineForge setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal static class InstallerEngine
{
    internal static void Install(Action<string>? progress)
    {
        ValidateInstallTarget(Program.InstallRoot);
        Directory.CreateDirectory(Program.ProgramsRoot);
        string staging = Program.InstallRoot + $".installing-{Guid.NewGuid():N}";
        string backup = Program.InstallRoot + ".backup";
        try
        {
            progress?.Invoke("Extracting application files…");
            Directory.CreateDirectory(staging);
            using Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("CineForge.Payload.zip")
                ?? throw new InvalidOperationException("The CineForge application payload is missing.");
            using var archive = new ZipArchive(payload, ZipArchiveMode.Read);
            archive.ExtractToDirectory(staging, overwriteFiles: true);
            string executable = Path.Combine(staging, "CineForge.exe");
            if (!File.Exists(executable)) throw new InvalidDataException("The installed application executable is missing.");

            progress?.Invoke("Finalizing the installation…");
            StopInstalledApp();
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
            if (Directory.Exists(Program.InstallRoot)) Directory.Move(Program.InstallRoot, backup);
            Directory.Move(staging, Program.InstallRoot);
            File.Copy(Environment.ProcessPath!, Path.Combine(Program.InstallRoot, "CineForge Setup.exe"), true);
            CreateShortcuts();
            RegisterUninstaller();
            Directory.CreateDirectory(Program.DataRoot);
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (!Directory.Exists(Program.InstallRoot) && Directory.Exists(backup)) Directory.Move(backup, Program.InstallRoot);
            throw;
        }
    }

    internal static void Uninstall()
    {
        ValidateInstallTarget(Program.InstallRoot);
        foreach (var process in Process.GetProcessesByName("CineForge"))
        {
            try { process.Kill(entireProcessTree: true); process.WaitForExit(5000); } catch { }
        }
        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CineForge Local.lnk"));
        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "CineForge Local.lnk"));
        Registry.CurrentUser.DeleteSubKeyTree(Program.UninstallKey, throwOnMissingSubKey: false);
        if (Directory.Exists(Program.InstallRoot)) Directory.Delete(Program.InstallRoot, true);
    }

    private static void StopInstalledApp()
    {
        foreach (var process in Process.GetProcessesByName("CineForge"))
        {
            try
            {
                string path = process.MainModule?.FileName ?? "";
                if (path.StartsWith(Program.InstallRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch { }
        }
    }

    internal static void Launch()
    {
        string executable = Path.Combine(Program.InstallRoot, "CineForge.exe");
        Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true, WorkingDirectory = Program.InstallRoot });
    }

    private static void RegisterUninstaller()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(Program.UninstallKey, true);
        string setup = Path.Combine(Program.InstallRoot, "CineForge Setup.exe");
        key.SetValue("DisplayName", Program.ProductName);
        key.SetValue("DisplayVersion", Program.ProductVersion);
        key.SetValue("Publisher", Program.Publisher);
        key.SetValue("DisplayIcon", $"\"{Path.Combine(Program.InstallRoot, "CineForge.exe")}\"");
        key.SetValue("InstallLocation", Program.InstallRoot);
        key.SetValue("UninstallString", $"\"{setup}\" --uninstall");
        key.SetValue("QuietUninstallString", $"\"{setup}\" --uninstall --silent");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        long bytes = Directory.EnumerateFiles(Program.InstallRoot, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
        key.SetValue("EstimatedSize", (int)Math.Min(int.MaxValue, bytes / 1024), RegistryValueKind.DWord);
    }

    private static void CreateShortcuts()
    {
        string target = Path.Combine(Program.InstallRoot, "CineForge.exe");
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CineForge Local.lnk"), target);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "CineForge Local.lnk"), target);
    }

    private static void CreateShortcut(string path, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Type shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows shortcut support is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(path);
        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = Program.InstallRoot;
        shortcut.IconLocation = target;
        shortcut.Description = "CineForge Local Cinematic Studio";
        shortcut.Save();
        Marshal.FinalReleaseComObject(shortcut);
        Marshal.FinalReleaseComObject(shell);
    }

    private static void DeleteShortcut(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void ValidateInstallTarget(string path)
    {
        string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        string allowedRoot = Path.GetFullPath(Program.ProgramsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !string.Equals(Path.GetFileName(full), Program.ProductName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The CineForge installation target failed its safety check.");
    }
}
