import json
import tempfile
import unittest
from pathlib import Path

from cineforge.config import Settings
from cineforge.discovery import discover_models
from cineforge.engine import NativeEngine
from cineforge.wan22 import PACK_FILES, PACK_ID, _rename_transformer_key


class Wan22NativeTests(unittest.TestCase):
    def test_official_checkpoint_key_maps_to_diffusers(self):
        self.assertEqual(
            _rename_transformer_key("blocks.0.self_attn.q.weight"),
            "blocks.0.attn1.to_q.weight",
        )
        self.assertEqual(
            _rename_transformer_key("head.head.weight"),
            "proj_out.weight",
        )

    def test_installed_scaled_fp8_pack_is_runnable(self):
        with tempfile.TemporaryDirectory() as folder:
            pack = Path(folder) / "pack"
            components = pack / "components"
            components.mkdir(parents=True)
            (pack / "cineforge-model.json").write_text(json.dumps({"id": PACK_ID}), encoding="utf-8")
            for filename in PACK_FILES.values():
                (components / filename).touch()
            catalog = discover_models(Settings(model_roots=[folder]))
            adapter = next(item for item in catalog["adapters"] if item["id"] == PACK_ID)
            self.assertTrue(adapter["available"])
            self.assertEqual(adapter["native_format"], "cineforge-wan22-scaled-fp8")

    def test_video_frames_are_normalized_to_wan_sequence(self):
        class Adapter:
            def generate(self, *_args, **_kwargs):
                return []

        engine = NativeEngine(Settings(), adapter=Adapter())
        queued = engine.queue_video(model_id=PACK_ID, prompt="test", length=75, quality="proof", seed=1)
        self.assertEqual(engine.jobs[queued["prompt_id"]].payload["length"], 73)
        engine.close()


if __name__ == "__main__":
    unittest.main()
