import time
import unittest

from cineforge.config import Settings
from cineforge.engine import NativeEngine, NativePipelineAdapter


class FakeAdapter:
    def generate(self, kind, model_id, payload, progress):
        progress(1, 2, "SAMPLING")
        progress(2, 2, "DECODING")
        return []


class NativeEngineTests(unittest.TestCase):
    def test_desktop_pipeline_disables_optional_content_checker(self):
        class Pipeline:
            safety_checker = object()

        pipeline = Pipeline()
        NativePipelineAdapter._configure_desktop_pipeline(pipeline)
        self.assertIsNone(pipeline.safety_checker)

    def test_native_job_reports_real_progress_without_comfy(self):
        engine = NativeEngine(Settings(), adapter=FakeAdapter())
        queued = engine.queue_still(
            prompt="test", negative_prompt="", model_id="native-test",
            width=64, height=64, quality="proof", seed=7,
        )
        deadline = time.time() + 2
        job = engine.history(queued["prompt_id"])
        while job["status"] not in {"complete", "error"} and time.time() < deadline:
            time.sleep(0.01)
            job = engine.history(queued["prompt_id"])
        self.assertEqual(job["status"], "complete")
        self.assertEqual(job["backend"], "native")
        self.assertEqual(job["value"], 2)
        self.assertEqual(job["phase"], "OUTPUT SAVED")


if __name__ == "__main__":
    unittest.main()
