import unittest
from unittest.mock import patch

from cineforge import config


class IsolatedStorageTests(unittest.TestCase):
    def test_default_model_roots_are_cineforge_only(self):
        with patch.dict(config.os.environ, {"CINEFORGE_MODEL_ROOT": ""}, clear=False):
            roots = config._default_model_roots()
        self.assertEqual(roots, [str(config.MODELS_ROOT)])
        self.assertFalse(any("shadowframe" in root.lower() for root in roots))

    def test_creative_folders_are_separate(self):
        self.assertEqual(config.UPLOADS_ROOT.name, "inputs")
        self.assertEqual(config.GENERATED_ROOT.name, "outputs")
        self.assertEqual(config.PROJECTS_ROOT.name, "projects")
        self.assertEqual(config.MODELS_ROOT.name, "models")


if __name__ == "__main__":
    unittest.main()
