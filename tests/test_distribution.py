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
        self.assertIn("CineForge-Desktop-Runtime-0.4.0-win-x64.zip", project)
        self.assertIn("RuntimeSha256", source)
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
        web = (ROOT / "web" / "index.html").read_text(encoding="utf-8")
        server = (ROOT / "cineforge" / "server.py").read_text(encoding="utf-8")
        self.assertIn("CineForge-Desktop-Setup", builder)
        self.assertIn("CineForge Desktop", metadata)
        self.assertIn("CineForge Desktop", web)
        self.assertIn('"edition": "desktop"', server)
        self.assertIn('"content_moderation": False', server)


if __name__ == "__main__":
    unittest.main()
