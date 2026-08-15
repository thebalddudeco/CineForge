import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


class DistributionTests(unittest.TestCase):
    def test_installer_downloads_from_cineforge_repository(self):
        source = (ROOT / "packaging" / "CineForge.Installer" / "Program.cs").read_text(encoding="utf-8")
        project = (ROOT / "packaging" / "CineForge.Installer" / "CineForge.Installer.csproj").read_text(encoding="utf-8")
        self.assertIn("TheBaldDudeCo/CineForge-Wan-Models", source)
        self.assertIn("RangeHeaderValue", source)
        self.assertIn("HashMatchesAsync", source)
        self.assertIn("CineForge Library", source)
        self.assertIn('ProductName = "CineForge Desktop"', source)
        self.assertIn("DownloadModelPackAsync", source)
        self.assertIn("downloaded and SHA-256 verified", source)
        self.assertIn("493b7c8ff0a451b6b4c049afb3e6396dbfa1c688", source)
        self.assertIn("support/tokenizer/tokenizer.json", source)
        self.assertIn("CineForge-Desktop-Runtime-5.1-win-x64.zip", project)
        self.assertIn("RuntimeSha256", source)
        self.assertIn("offset == RuntimeBytes", source)
        self.assertIn("offset == file.Bytes", source)
        self.assertIn("RequestedRangeNotSatisfiable", source)
        self.assertNotIn('ModelRevision = "main"', source)

    def test_runtime_does_not_discover_shadowframe(self):
        checked = [
            ROOT / "cineforge" / "config.py",
            ROOT / "config.example.json",
            ROOT / "run.ps1",
            ROOT / "packaging" / "Build-Release.ps1",
        ]
        combined = "\n".join(path.read_text(encoding="utf-8") for path in checked).lower()
        self.assertNotIn("shadowframe", combined)

    def test_release_builder_bootstraps_independent_cuda_runtime(self):
        source = (ROOT / "packaging" / "Build-Release.ps1").read_text(encoding="utf-8")
        requirements = (ROOT / "packaging" / "requirements-native.txt").read_text(encoding="utf-8")
        self.assertIn("torch==2.10.0+cu130", source)
        self.assertIn("download.pytorch.org/whl/cu130", source)
        self.assertIn("diffusers==0.39.0", requirements)
        self.assertIn("transformers==5.0.0", requirements)
        self.assertIn("CineForgeRuntimeSha256=$runtimeSha256", source)
        self.assertIn("CineForgeRuntimeBytes=$runtimeBytes", source)

    def test_desktop_release_identity(self):
        builder = (ROOT / "packaging" / "Build-Release.ps1").read_text(encoding="utf-8")
        metadata = (ROOT / "packaging" / "version_info.txt").read_text(encoding="utf-8")
        desktop = (ROOT / "desktop" / "CineForge.Desktop" / "CineForge.Desktop.csproj").read_text(encoding="utf-8")
        worker = (ROOT / "cineforge" / "worker.py").read_text(encoding="utf-8")
        self.assertIn("CineForge-Desktop-Setup", builder)
        self.assertIn("CineForge Desktop", metadata)
        self.assertIn("<UseWPF>true</UseWPF>", desktop)
        self.assertIn('"edition": "desktop"', worker)

    def test_desktop_uses_native_window_and_private_process_transport(self):
        builder = (ROOT / "packaging" / "Build-Release.ps1").read_text(encoding="utf-8")
        client = (ROOT / "desktop" / "CineForge.Desktop" / "EngineClient.cs").read_text(encoding="utf-8")
        worker = (ROOT / "cineforge" / "worker.py").read_text(encoding="utf-8")
        installer = (ROOT / "packaging" / "CineForge.Installer" / "Program.cs").read_text(encoding="utf-8")
        self.assertIn('dotnet publish $desktopProject', builder)
        self.assertIn('--name "CineForge Engine"', builder)
        self.assertNotIn("--add-data \"$(Join-Path $appRoot 'web')", builder)
        self.assertIn("RedirectStandardInput = true", client)
        self.assertIn("RedirectStandardOutput = true", client)
        self.assertIn("never binds a network port", worker)
        self.assertNotIn('host = "127.0.0.1"', installer)
        self.assertNotIn("7331", installer)
        self.assertFalse((ROOT / "web").joinpath("index.html").exists())

    def test_approved_brand_system_is_native_and_packaged(self):
        xaml = (ROOT / "desktop" / "CineForge.Desktop" / "MainWindow.xaml").read_text(encoding="utf-8")
        app = (ROOT / "desktop" / "CineForge.Desktop" / "App.xaml").read_text(encoding="utf-8")
        project = (ROOT / "desktop" / "CineForge.Desktop" / "CineForge.Desktop.csproj").read_text(encoding="utf-8")
        code = (ROOT / "desktop" / "CineForge.Desktop" / "MainWindow.xaml.cs").read_text(encoding="utf-8")
        self.assertIn('<RowDefinition Height="88"/>', xaml)
        self.assertIn('Width="49" Height="44"', xaml)
        self.assertIn('Points="6,1 43,1 49,10 30,43 20,43 0,10"', xaml)
        self.assertIn('Text="v5.1"', xaml)
        self.assertIn('x:Name="RuntimeCanvas"', xaml)
        self.assertIn('x:Name="MatrixCanvas"', xaml)
        self.assertIn('x:Name="GenerationJobLabel"', xaml)
        self.assertIn('x:Key="MicroGrid"', app)
        self.assertIn('TextElement.Foreground="{TemplateBinding Foreground}"', app)
        self.assertIn('Assets\\Fonts\\*.ttf', project)
        self.assertIn("DrawRuntimeSignal", code)
        for name in ("Anta-Regular.ttf", "CutiveMono-Regular.ttf", "InterTight-VariableFont_wght.ttf"):
            self.assertTrue((ROOT / "desktop" / "CineForge.Desktop" / "Assets" / "Fonts" / name).is_file())

    def test_desktop_generation_controls_follow_the_visible_workflow(self):
        xaml = (ROOT / "desktop" / "CineForge.Desktop" / "MainWindow.xaml").read_text(encoding="utf-8")
        code = (ROOT / "desktop" / "CineForge.Desktop" / "MainWindow.xaml.cs").read_text(encoding="utf-8")
        model_picker = xaml.index('x:Name="ModelPicker"')
        reference_pack = xaml.index('Text="{DynamicResource L.ReferencePanelLabel}"')
        build_button = xaml.index('x:Name="BuildButton"')
        factory_panel = xaml.index('x:Name="FactoryPanel"')
        self.assertLess(model_picker, reference_pack)
        self.assertLess(reference_pack, build_button)
        self.assertLess(build_button, factory_panel)
        self.assertEqual(xaml.count('x:Name="ModelPicker"'), 1)
        self.assertIn('x:Name="BuildButton" Content="{DynamicResource L.BuildFactory}" Style="{StaticResource PrimaryButton}" Height="46" IsEnabled="False"', xaml)
        self.assertIn('BuildButton.IsEnabled = _referenceImage is not null;', code)
        self.assertIn('FactoryPanel.BringIntoView()', code)

    def test_native_palette_and_light_surface_contrast_are_locked(self):
        app = (ROOT / "desktop" / "CineForge.Desktop" / "App.xaml").read_text(encoding="utf-8")
        for color in ("#E4FF1A", "#242424", "#020300", "#E0E0E0", "#89FC00"):
            self.assertIn(color, app)
        self.assertIn('<Setter Property="Foreground" Value="{StaticResource DarkText}"/>', app)
        self.assertIn('<Setter Property="Background" Value="{StaticResource Acid}"/>', app)
        self.assertIn('SystemColors.HighlightTextBrushKey', app)
        self.assertIn('Color="{StaticResource BlackColor}"', app)

    def test_desktop_has_persistent_english_korean_japanese_localization(self):
        desktop = ROOT / "desktop" / "CineForge.Desktop"
        xaml = (desktop / "MainWindow.xaml").read_text(encoding="utf-8")
        manager = (desktop / "LocalizationManager.cs").read_text(encoding="utf-8")
        installer = (ROOT / "packaging" / "CineForge.Installer" / "Program.cs").read_text(encoding="utf-8")
        for language in ("en", "ko", "ja"):
            strings = desktop / "Localization" / f"Strings.{language}.xaml"
            self.assertTrue(strings.is_file())
            self.assertIn('x:Key="L.BuildSystem"', strings.read_text(encoding="utf-8"))
        for font in (
            "Gugi-Regular.ttf", "Orbit-Regular.ttf", "IBMPlexSansKR-Regular.ttf",
            "MPLUS1-VariableFont_wght.ttf", "ZenKurenaido-Regular.ttf",
            "ZenKakuGothicAntique-Regular.ttf", "SairaCondensed-Regular.ttf",
        ):
            self.assertTrue((desktop / "Assets" / "Fonts" / font).is_file())
        self.assertIn('Content="한" Tag="ko"', xaml)
        self.assertIn('Content="日" Tag="ja"', xaml)
        self.assertIn('LocalizationManager.Apply(language, persist: true)', (desktop / "MainWindow.xaml.cs").read_text(encoding="utf-8"))
        self.assertIn('PreferencesPath', manager)
        self.assertIn('LANGUAGE / 언어 / 言語', installer)
        self.assertIn('version = Program.ProductVersion, language', installer)


if __name__ == "__main__":
    unittest.main()
