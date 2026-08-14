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


if __name__ == "__main__":
    unittest.main()
