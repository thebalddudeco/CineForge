import unittest

from cineforge.worker import Worker


class _Engine:
    def runtime(self):
        return {"online": True, "device": "Test GPU"}

    def models(self):
        return {"adapters": []}

    def history(self, job_id):
        return {"prompt_id": job_id, "status": "running"}

    def close(self):
        pass


class NativeWorkerTests(unittest.TestCase):
    def setUp(self):
        self.worker = Worker.__new__(Worker)
        self.worker.engine = _Engine()
        self.worker.stopping = False

    def test_health_identifies_private_process_transport(self):
        result = self.worker.dispatch("health", {})
        self.assertEqual(result["transport"], "private-process")
        self.assertNotIn("url", result)

    def test_job_status_is_forwarded_without_http(self):
        result = self.worker.dispatch("job", {"job_id": "job-1"})
        self.assertEqual(result["prompt_id"], "job-1")


if __name__ == "__main__":
    unittest.main()
