using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CineForge.Desktop;

internal sealed class EngineClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process? _process;
    private int _requestId;

    public async Task StartAsync()
    {
        if (_process is { HasExited: false }) return;
        var baseDir = AppContext.BaseDirectory;
        var bundled = Path.Combine(baseDir, "Engine", "CineForge Engine.exe");
        ProcessStartInfo start;
        if (File.Exists(bundled))
        {
            start = new ProcessStartInfo(bundled);
        }
        else
        {
            var repo = FindRepository(baseDir) ?? throw new FileNotFoundException("The bundled CineForge Engine was not found.");
            start = new ProcessStartInfo("python", $"\"{Path.Combine(repo, "cineforge_worker_entry.py")}\"")
            {
                WorkingDirectory = repo
            };
        }
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.WindowStyle = ProcessWindowStyle.Hidden;
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.StandardInputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        start.StandardOutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        start.Environment["PYTHONUTF8"] = "1";
        var dataRoot = ResolveInstalledDataRoot();
        if (!string.IsNullOrWhiteSpace(dataRoot)) start.Environment["CINEFORGE_DATA_ROOT"] = dataRoot;
        _process = Process.Start(start) ?? throw new InvalidOperationException("CineForge Engine could not start.");
        _ = Task.Run(async () =>
        {
            while (_process is { HasExited: false }) await _process.StandardError.ReadLineAsync();
        });
        await SendAsync("health");
    }

    public async Task<JsonElement> SendAsync(string command, object? payload = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (_process is not { HasExited: false }) throw new InvalidOperationException("CineForge Engine is offline.");
            var id = Interlocked.Increment(ref _requestId);
            var message = JsonSerializer.Serialize(new { request_id = id, command, payload = payload ?? new { } });
            await _process.StandardInput.WriteLineAsync(message);
            await _process.StandardInput.FlushAsync();
            string? line;
            JsonDocument? document = null;
            while ((line = await _process.StandardOutput.ReadLineAsync()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try { document = JsonDocument.Parse(line); }
                catch (JsonException) { continue; }
                if (document.RootElement.TryGetProperty("request_id", out var responseId) && responseId.ValueKind == JsonValueKind.Number && responseId.GetInt32() == id) break;
                document.Dispose();
                document = null;
            }
            if (document is null) throw new InvalidOperationException("CineForge Engine stopped responding.");
            using (document)
            {
                var root = document.RootElement;
                if (!root.GetProperty("ok").GetBoolean())
                    throw new InvalidOperationException($"{command}: {(root.TryGetProperty("error", out var error) ? error.GetString() : "Engine request failed.")}");
                return root.GetProperty("result").Clone();
            }
        }
        finally { _gate.Release(); }
    }

    private static string? FindRepository(string from)
    {
        var current = new DirectoryInfo(from);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "cineforge_worker_entry.py"))) return current.FullName;
            current = current.Parent;
        }
        var explicitRoot = Environment.GetEnvironmentVariable("CINEFORGE_SOURCE_ROOT");
        return explicitRoot is not null && File.Exists(Path.Combine(explicitRoot, "cineforge_worker_entry.py")) ? explicitRoot : null;
    }

    internal static string? ResolveInstalledDataRoot()
    {
        var explicitRoot = Environment.GetEnvironmentVariable("CINEFORGE_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(explicitRoot)) return explicitRoot;
        var pointer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CineForge", "install.json");
        if (!File.Exists(pointer)) return null;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(pointer));
            return document.RootElement.TryGetProperty("data_root", out var root) ? root.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is null) return;
        try
        {
            if (!_process.HasExited)
            {
                await SendAsync("shutdown");
                if (!await Task.Run(() => _process.WaitForExit(2000))) _process.Kill(true);
            }
        }
        catch { if (!_process.HasExited) _process.Kill(true); }
        _process.Dispose();
        _gate.Dispose();
    }
}
