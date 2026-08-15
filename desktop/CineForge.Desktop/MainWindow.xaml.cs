using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Windows.Input;
using IOPath = System.IO.Path;

namespace CineForge.Desktop;

public partial class MainWindow : Window
{
    private readonly EngineClient _engine = new();
    private readonly ObservableCollection<ShotView> _visibleShots = [];
    private readonly Dictionary<string, List<ShotView>> _branches = [];
    private readonly DispatcherTimer _matrixTimer;
    private readonly DispatcherTimer _runtimeTimer;
    private readonly DispatcherTimer _telemetryTimer;
    private readonly Random _random = new(2049);
    private readonly List<MatrixDot> _dots = [];
    private readonly List<Queue<double>> _runtimeSamples = [new(), new(), new(), new()];
    private string? _referenceImage;
    private string? _resultPath;
    private string? _activeJob;
    private DateTime _jobStarted;
    private bool _closing;
    private double _runtimePhase;
    private bool _samplingRuntime;

    public MainWindow()
    {
        InitializeComponent();
        ShotList.ItemsSource = _visibleShots;
        // 72 prototype frames across a five-second breathing cycle (~14.4 fps).
        _matrixTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000d / 14.4d), DispatcherPriority.Render, DrawMatrix, Dispatcher);
        _runtimeTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(90), DispatcherPriority.Render, DrawRuntimeSignal, Dispatcher);
        _telemetryTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, SampleRuntime, Dispatcher);
        Loaded += OnLoaded;
        Closing += OnClosing;
        MatrixCanvas.SizeChanged += (_, _) => BuildMatrix();
        RuntimeCanvas.SizeChanged += (_, _) => DrawRuntimeSignal(null, EventArgs.Empty);
        UpdateLanguageButtons();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Environment.GetEnvironmentVariable("CINEFORGE_UI_PREVIEW") == "1")
        {
            RuntimeHeader.Text = "CONNECTED / NVIDIA GEFORCE RTX 4070";
            RuntimeGpuValue.Text = "RTX 4070";
            RuntimeVramValue.Text = "83%";
            RuntimeEngineValue.Text = "NATIVE WAN / CUDA";
            RuntimeBuildValue.Text = "0.5.0";
            RuntimeOrbital.Activity = 42;
            SeedPreviewTelemetry();
            StatusText.Text = "NATIVE UI PREVIEW / ENGINE BYPASSED";
            _runtimeTimer.Start();
            _telemetryTimer.Start();
            var reviewPanel = Environment.GetEnvironmentVariable("CINEFORGE_UI_REVIEW_PANEL");
            if (reviewPanel == "generation")
            {
                GenerationPanel.Visibility = Visibility.Visible;
                GenerationProgress.Value = 73;
                GenerationPercent.Text = "73%";
                GenerationStep.Text = "STEP 31 / 42";
                GenerationPhase.Text = "NATIVE WAN MODEL INFERENCE";
                ElapsedLabel.Text = "00:48";
                EtaLabel.Text = "00:18";
                _jobStarted = DateTime.UtcNow;
                await Dispatcher.InvokeAsync(BuildMatrix, DispatcherPriority.Loaded);
                _matrixTimer.Start();
            }
            return;
        }
        try
        {
            StatusText.Text = L("L.StatusStarting");
            await _engine.StartAsync();
            var health = await _engine.SendAsync("health");
            var runtime = health.GetProperty("runtime");
            var online = runtime.TryGetProperty("online", out var state) && state.GetBoolean();
            var device = runtime.TryGetProperty("device", out var gpu) ? gpu.GetString() : "NVIDIA GPU";
            var totalVram = runtime.TryGetProperty("vram_total_gb", out var totalNode) ? totalNode.GetDouble() : 0;
            var freeVram = runtime.TryGetProperty("vram_free_gb", out var freeNode) ? freeNode.GetDouble() : 0;
            var usedPercent = totalVram > 0 ? Math.Clamp((totalVram - freeVram) / totalVram * 100, 0, 100) : 0;
            RuntimeHeader.Text = online ? string.Format(L("L.Connected"), device) : L("L.EngineReadyNoGpu");
            ApplyRuntimeTelemetry(runtime);
            _runtimeTimer.Start();
            _telemetryTimer.Start();
            StatusText.Text = online ? L("L.NativeOnline") : L("L.CheckGpu");
            await LoadModelsAsync();
        }
        catch (Exception ex)
        {
            RuntimeHeader.Text = L("L.EngineOffline");
            StatusText.Text = ex.Message.ToUpperInvariant();
            MessageBox.Show(ex.Message, "CineForge Desktop", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadModelsAsync()
    {
        var catalog = await _engine.SendAsync("models");
        var models = new List<ModelOption>();
        foreach (var adapter in catalog.GetProperty("adapters").EnumerateArray())
        {
            if (adapter.GetProperty("kind").GetString() != "video") continue;
            if (!adapter.TryGetProperty("available", out var available) || !available.GetBoolean()) continue;
            models.Add(new ModelOption(adapter.GetProperty("id").GetString()!, adapter.GetProperty("label").GetString()!));
        }
        ModelPicker.ItemsSource = models;
        if (models.Count > 0) ModelPicker.SelectedIndex = 0;
        else StatusText.Text = L("L.NoModel");
    }

    private async void RefreshModels_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "REFRESHING LOCAL WAN MODELS";
            await LoadModelsAsync();
            StatusText.Text = "LOCAL WAN MODELS UPDATED";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void SelectReference_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = L("L.ChooseReferenceTitle"),
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            StatusText.Text = L("L.LockingReference");
            var result = await _engine.SendAsync("import_reference", new { path = dialog.FileName, name = $"canonical-{IOPath.GetFileName(dialog.FileName)}" });
            _referenceImage = result.GetProperty("asset").GetProperty("path").GetString();
            ReferencePath.Text = _referenceImage?.ToUpperInvariant();
            StatusText.Text = L("L.ReferenceLocked");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void BuildPlan_Click(object sender, RoutedEventArgs e)
    {
        BuildButton.IsEnabled = false;
        BuildButton.Content = L("L.BuildingFactory");
        try
        {
            var project = await _engine.SendAsync("plan", new
            {
                title = TitleInput.Text,
                subject = SubjectInput.Text,
                action = ActionInput.Text,
                environment = EnvironmentInput.Text,
                objective = ObjectiveInput.Text,
                obstacle = ObstacleInput.Text,
                lighting = LightingInput.Text,
                look = LookInput.Text,
                duration = 5,
                continuity = "identity, wardrobe, props, geography, weather, time of day, and screen direction"
            });
            _branches.Clear();
            var branches = project.GetProperty("branches");
            foreach (var branchName in new[] { "angles", "inserts", "progression" })
            {
                var items = new List<ShotView>();
                foreach (var shot in branches.GetProperty(branchName).EnumerateArray())
                {
                    items.Add(new ShotView(
                        shot.GetProperty("id").GetString()!,
                        branchName,
                        shot.GetProperty("index").GetInt32(),
                        shot.GetProperty("title").GetString()!,
                        shot.GetProperty("story_change").GetString()!,
                        shot.GetProperty("prompt").GetString()!,
                        shot.GetProperty("motion_prompt").GetString()!));
                }
                _branches[branchName] = items;
            }
            ProjectIdLabel.Text = $"PROJECT {project.GetProperty("project_id").GetString()} · 90-NODE CINEMATIC LOGIC / NATIVE DESKTOP";
            ShowBranch("angles");
            FactoryPanel.Visibility = Visibility.Visible;
            StatusText.Text = L("L.FactoryReady");
        }
        catch (Exception ex) { ShowError(ex); }
        finally
        {
            BuildButton.IsEnabled = true;
            BuildButton.Content = L("L.BuildFactory");
        }
    }

    private void Branch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string branch }) ShowBranch(branch);
    }

    private void ShowBranch(string branch)
    {
        _visibleShots.Clear();
        if (_branches.TryGetValue(branch, out var shots)) foreach (var shot in shots) _visibleShots.Add(shot);
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ShotView shot }) return;
        if (_referenceImage is null)
        {
            MessageBox.Show(L("L.ReferenceRequired"), L("L.ReferenceRequiredTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (ModelPicker.SelectedItem is not ModelOption model)
        {
            MessageBox.Show(L("L.ModelRequired"), L("L.ModelRequiredTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            GenerationPanel.Visibility = Visibility.Visible;
            OpenResultButton.Visibility = Visibility.Collapsed;
            GenerationTitle.Text = L("L.Generating");
            GenerationTitle.Foreground = (Brush)FindResource("Acid");
            GenerationProgress.Value = 0;
            GenerationPercent.Text = "00%";
            GenerationPhase.Text = L("L.Queueing");
            JobDetail.Text = $"{shot.Branch.ToUpperInvariant()} / {shot.IndexLabel} · {model.Label.ToUpperInvariant()}";
            _resultPath = null;
            _jobStarted = DateTime.UtcNow;
            BuildMatrix();
            _matrixTimer.Start();
            var queued = await _engine.SendAsync("queue_video", new
            {
                image_path = _referenceImage,
                prompt = shot.MotionPrompt,
                negative_prompt = "identity drift, morphing, sliding feet, floating objects, text, watermark, deformed hands",
                width = 768,
                height = 432,
                length = 81,
                quality = "final",
                model_id = model.Id
            });
            _activeJob = queued.GetProperty("prompt_id").GetString();
            GenerationJobLabel.Text = $"JOB / CF-{_activeJob?[..Math.Min(6, _activeJob.Length)].ToUpperInvariant()}";
            await PollJobAsync(_activeJob!);
        }
        catch (Exception ex) { ShowGenerationError(ex); }
    }

    private async Task PollJobAsync(string jobId)
    {
        while (!_closing && _activeJob == jobId)
        {
            await Task.Delay(900);
            var job = await _engine.SendAsync("job", new { job_id = jobId });
            var status = job.GetProperty("status").GetString();
            var value = job.TryGetProperty("value", out var current) ? current.GetInt32() : 0;
            var maximum = job.TryGetProperty("max", out var max) ? max.GetInt32() : 0;
            var percent = maximum > 0 ? Math.Clamp(value * 100d / maximum, 0, 100) : 0;
            GenerationProgress.Value = percent;
            GenerationPercent.Text = $"{percent:00}%";
            GenerationPhase.Text = job.TryGetProperty("phase", out var phase) ? phase.GetString()?.ToUpperInvariant() : L("L.Running");
            GenerationStep.Text = maximum > 0 ? string.Format(L("L.Step"), value, maximum) : string.Format(L("L.Step"), "—", "—");
            var elapsed = job.TryGetProperty("elapsed_seconds", out var elapsedNode) ? elapsedNode.GetDouble() : (DateTime.UtcNow - _jobStarted).TotalSeconds;
            var eta = job.TryGetProperty("eta_seconds", out var etaNode) ? etaNode.GetDouble() : 0;
            ElapsedLabel.Text = FormatClock(elapsed);
            EtaLabel.Text = eta > 0 ? FormatClock(eta) : "—";
            if (status == "complete")
            {
                GenerationProgress.Value = 100;
                GenerationPercent.Text = "100%";
                GenerationTitle.Text = L("L.Complete");
                GenerationPhase.Text = L("L.OutputSaved");
                var outputs = job.GetProperty("outputs");
                if (outputs.GetArrayLength() > 0) _resultPath = outputs[0].GetProperty("path").GetString();
                OpenResultButton.Visibility = _resultPath is null ? Visibility.Collapsed : Visibility.Visible;
                StatusText.Text = L("L.VideoComplete");
                _matrixTimer.Stop();
                _activeJob = null;
                return;
            }
            if (status == "error") throw new InvalidOperationException(job.GetProperty("error").GetString());
        }
    }

    private void BuildMatrix()
    {
        if (MatrixCanvas.ActualWidth < 20 || MatrixCanvas.ActualHeight < 20) return;
        MatrixCanvas.Children.Clear();
        _dots.Clear();
        const int columns = 38, rows = 10;
        var pitchX = MatrixCanvas.ActualWidth / columns;
        var pitchY = MatrixCanvas.ActualHeight / rows;
        for (var row = 0; row < rows; row++)
        for (var column = 0; column < columns; column++)
        {
            var size = Math.Max(3, Math.Min(7, Math.Min(pitchX, pitchY) * .52));
            var square = new Rectangle { Width = size, Height = size, Fill = (Brush)FindResource("Ink") };
            Canvas.SetLeft(square, column * pitchX + (pitchX - size) / 2);
            Canvas.SetTop(square, row * pitchY + (pitchY - size) / 2);
            MatrixCanvas.Children.Add(square);
            _dots.Add(new MatrixDot(square, _random.NextDouble() * Math.PI * 2, _random.NextDouble()));
        }
    }

    private void DrawMatrix(object? sender, EventArgs e)
    {
        if (_dots.Count == 0) return;
        var seconds = (DateTime.UtcNow - _jobStarted).TotalSeconds;
        const double cycleSeconds = 5.0;
        var greenCycle = (int)(seconds / .9);
        foreach (var (dot, index) in _dots.Select((value, index) => (value, index)))
        {
            // Every cell uses the same exact curve and duration. Only its
            // randomized starting phase differs, producing the asynchronous field.
            var breath = .5 - .5 * Math.Cos((seconds / cycleSeconds * Math.PI * 2) + dot.Phase);
            dot.Shape.Opacity = breath;
            var green = ((index * 97 + greenCycle * 53) % _dots.Count) < 6;
            dot.Shape.Fill = green ? (Brush)FindResource("Acid") : (Brush)FindResource("Ink");
        }
    }

    private void DrawRuntimeSignal(object? sender, EventArgs e)
    {
        _runtimePhase += .045;
        DrawSignalCanvas(RuntimeCanvas, compact: false);
    }

    private void DrawSignalCanvas(Canvas canvas, bool compact)
    {
        if (canvas.ActualWidth < 30 || canvas.ActualHeight < 24) return;
        canvas.Children.Clear();
        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        var gridBrush = (Brush)FindResource("Panel");
        for (var x = 0d; x < width; x += 20)
            canvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = height, Stroke = gridBrush, StrokeThickness = .55 });
        for (var y = 0d; y < height; y += 20)
            canvas.Children.Add(new Line { X1 = 0, X2 = width, Y1 = y, Y2 = y, Stroke = gridBrush, StrokeThickness = .55 });

        var colors = new[] { (Brush)FindResource("Acid"), (Brush)FindResource("Ink"), (Brush)FindResource("Ink"), (Brush)FindResource("Ink") };
        var opacities = new[] { 1d, .68d, .92d, .46d };
        var bands = compact ? 1 : 4;
        for (var band = 0; band < bands; band++)
        {
            var line = new Polyline { Stroke = colors[band], StrokeThickness = band == 0 ? 1.25 : .8, Opacity = opacities[band] };
            var baseline = compact ? height * .55 : height * (.16 + band * .235);
            var samples = _runtimeSamples[band].ToArray();
            if (samples.Length < 2) samples = Enumerable.Repeat(50d, 49).ToArray();
            for (var i = 0; i < samples.Length; i++)
            {
                var px = width * i / Math.Max(1, samples.Length - 1);
                var amplitude = compact ? 4.2 : Math.Max(2.5, height * .07);
                var wave = (50 - samples[i]) / 50 * amplitude;
                line.Points.Add(new Point(px, baseline + wave));
            }
            canvas.Children.Add(line);
        }
        var scanX = (_runtimePhase * 18) % width;
        canvas.Children.Add(new Line { X1 = scanX, X2 = scanX, Y1 = 0, Y2 = height, Stroke = (Brush)FindResource("Acid"), StrokeThickness = 1, Opacity = .8 });
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void SampleRuntime(object? sender, EventArgs e)
    {
        if (Environment.GetEnvironmentVariable("CINEFORGE_UI_PREVIEW") == "1")
        {
            var t = DateTime.UtcNow.TimeOfDay.TotalSeconds;
            var utilization = 42 + Math.Sin(t * .53) * 24 + Math.Sin(t * 1.17) * 7;
            AddRuntimeSample(0, utilization);
            RuntimeOrbital.Activity = utilization;
            AddRuntimeSample(1, 83 + Math.Sin(t * .19) * 3);
            AddRuntimeSample(2, 58 + Math.Sin(t * .31) * 8);
            AddRuntimeSample(3, 44 + Math.Sin(t * .41) * 18);
            return;
        }
        if (_samplingRuntime || _closing) return;
        _samplingRuntime = true;
        try
        {
            var health = await _engine.SendAsync("health");
            ApplyRuntimeTelemetry(health.GetProperty("runtime"));
        }
        catch { /* Preserve the last valid sample during a transient probe failure. */ }
        finally { _samplingRuntime = false; }
    }

    private void ApplyRuntimeTelemetry(JsonElement runtime)
    {
        var online = runtime.TryGetProperty("online", out var onlineNode) && onlineNode.GetBoolean();
        var device = runtime.TryGetProperty("device", out var deviceNode) ? deviceNode.GetString() ?? "NVIDIA GPU" : "NVIDIA GPU";
        var total = runtime.TryGetProperty("vram_total_gb", out var totalNode) ? totalNode.GetDouble() : 0;
        var free = runtime.TryGetProperty("vram_free_gb", out var freeNode) ? freeNode.GetDouble() : 0;
        var vram = total > 0 ? Math.Clamp((total - free) / total * 100, 0, 100) : 0;
        var utilization = runtime.TryGetProperty("gpu_utilization_percent", out var utilizationNode) ? utilizationNode.GetDouble() : 0;
        var temperature = runtime.TryGetProperty("temperature_c", out var temperatureNode) ? temperatureNode.GetDouble() : 0;
        var power = runtime.TryGetProperty("power_w", out var powerNode) ? powerNode.GetDouble() : 0;
        var build = runtime.TryGetProperty("engine_version", out var buildNode) ? buildNode.GetString() ?? "0.5.0" : "0.5.0";

        RuntimeHeader.Text = online ? "CONNECTED / 01" : "OFFLINE / 00";
        RuntimeGpuValue.Text = device.Replace("NVIDIA GeForce ", "");
        RuntimeVramValue.Text = total > 0 ? $"{vram:0}%" : "—";
        RuntimeEngineValue.Text = "NATIVE WAN / CUDA";
        RuntimeBuildValue.Text = $"BUILD {build}";
        RuntimeOrbital.Activity = utilization;
        AddRuntimeSample(0, utilization);
        AddRuntimeSample(1, vram);
        AddRuntimeSample(2, Math.Clamp(temperature, 0, 100));
        AddRuntimeSample(3, Math.Clamp(power / 3.5, 0, 100));
    }

    private void AddRuntimeSample(int band, double value)
    {
        var samples = _runtimeSamples[band];
        if (samples.Count == 0)
        {
            for (var index = 0; index < 64; index++) samples.Enqueue(Math.Clamp(value, 0, 100));
            return;
        }
        samples.Enqueue(Math.Clamp(value, 0, 100));
        while (samples.Count > 64) samples.Dequeue();
    }

    private void SeedPreviewTelemetry()
    {
        for (var index = 0; index < 64; index++)
        {
            AddRuntimeSample(0, 42 + Math.Sin(index * .34) * 24 + Math.Sin(index * .79) * 6);
            AddRuntimeSample(1, 83 + Math.Sin(index * .18) * 3);
            AddRuntimeSample(2, 58 + Math.Sin(index * .28 + 1.4) * 9);
            AddRuntimeSample(3, 44 + Math.Sin(index * .42 + 2.1) * 18);
        }
    }

    private void Language_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string language }) return;
        LocalizationManager.Apply(language, persist: true);
        Language = XmlLanguage.GetLanguage(language switch { "ko" => "ko-KR", "ja" => "ja-JP", _ => "en-US" });
        UpdateLanguageButtons();
        StatusText.Text = L("L.LanguageChanged");
        if (_activeJob is not null) GenerationTitle.Text = L("L.Generating");
    }

    private void UpdateLanguageButtons()
    {
        foreach (var button in new[] { EnglishLanguageButton, KoreanLanguageButton, JapaneseLanguageButton })
        {
            var active = Equals(button.Tag, LocalizationManager.CurrentLanguage);
            button.Background = (Brush)FindResource(active ? "Acid" : "Panel");
            button.Foreground = (Brush)FindResource(active ? "DarkText" : "Ink");
            button.BorderBrush = (Brush)FindResource(active ? "Acid" : "Line");
        }
    }

    private static string L(string key) => LocalizationManager.Text(key);

    private void OpenResult_Click(object sender, RoutedEventArgs e)
    {
        if (_resultPath is null || !File.Exists(_resultPath)) return;
        Process.Start(new ProcessStartInfo(_resultPath) { UseShellExecute = true });
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        var root = EngineClient.ResolveInstalledDataRoot();
        if (string.IsNullOrWhiteSpace(root)) root = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CineForge");
        var output = IOPath.Combine(root, "outputs");
        Directory.CreateDirectory(output);
        Process.Start(new ProcessStartInfo(output) { UseShellExecute = true });
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closing) return;
        _closing = true;
        _matrixTimer.Stop();
        _runtimeTimer.Stop();
        _telemetryTimer.Stop();
        await _engine.DisposeAsync();
    }

    private void ShowError(Exception ex)
    {
        StatusText.Text = ex.Message.ToUpperInvariant();
        MessageBox.Show(ex.Message, "CineForge Desktop", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowGenerationError(Exception ex)
    {
        _activeJob = null;
        _matrixTimer.Stop();
        GenerationTitle.Text = L("L.GenerationError");
        GenerationTitle.Foreground = (Brush)FindResource("Signal");
        GenerationPhase.Text = ex.Message.ToUpperInvariant();
        ShowError(ex);
    }

    private static string FormatClock(double seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return duration.TotalHours >= 1 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");
    }

    private sealed record MatrixDot(Rectangle Shape, double Phase, double Seed);
}

public sealed record ModelOption(string Id, string Label);

public sealed record ShotView(string Id, string Branch, int Index, string Title, string StoryChange, string Prompt, string MotionPrompt)
{
    public string IndexLabel => $"{Index + 1:00}";
}
