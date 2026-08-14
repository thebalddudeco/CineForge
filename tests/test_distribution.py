import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


class DistributionTests(unittest.TestCase):
    def test_installer_downloads_from_cineforge_repository(self):
        source = (ROOT / "packaging" / "CineForge.Installer" / "Program.cs").read_text(encoding="utf-8")
        self.assertIn("TheBaldDudeCo/CineForge-Wan-Models", source)
        self.assertIn("RangeHeaderValue", source)
        self.assertIn("HashMatchesAsync", source)
        self.assertIn("CineForge Library", source)
        self.assertIn('ProductName = "CineForge Desktop"', source)
        self.assertIn("automatically downloads and verifies", source)
        self.assertIn("3abefe070febb87cf51e038edda29934743639fb", source)
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

    def test_desktop_release_identity(self):
        builder = (ROOT / "packaging" / "Build-Release.ps1").read_text(encoding="utf-8")
        metadata = (ROOT / "packaging" / "version_info.txt").read_text(encoding="utf-8")
        web = (ROOT / "web" / "index.html").read_text(encoding="utf-8")
        self.assertIn("CineForge-Desktop-Setup", builder)
        self.assertIn("CineForge Desktop", metadata)
        self.assertIn("CineForge Desktop", web)


if __name__ == "__main__":
    unittest.main()
