import unittest
from unittest.mock import patch

from cineforge.config import Settings
from cineforge.discovery import runtime_status


class RuntimeStatusTests(unittest.TestCase):
    @patch("cineforge.discovery._get_json")
    def test_free_vram_is_clamped_to_physical_total(self, get_json):
        get_json.return_value = {
            "system": {"comfyui_version": "0.30.0"},
            "devices": [{
                "name": "cuda:0 NVIDIA GeForce RTX 4070 : cudaMallocAsync",
                "vram_total": 12 * 1024**3,
                "vram_free": 15 * 1024**3,
            }],
        }
        result = runtime_status(Settings())
        self.assertEqual(result["vram_total_gb"], 12.0)
        self.assertEqual(result["vram_free_gb"], 12.0)


if __name__ == "__main__":
    unittest.main()
