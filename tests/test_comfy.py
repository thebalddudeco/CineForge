import unittest

from cineforge.comfy import ltx_workflow, wan22_workflow


class WorkflowTests(unittest.TestCase):
    def test_ltx_workflow_is_complete(self):
        workflow = ltx_workflow("frame.png", "a restrained glance", "morphing", 768, 432, 25, 42, 12)
        self.assertEqual(workflow["1"]["inputs"]["image"], "frame.png")
        self.assertEqual(workflow["7"]["inputs"]["length"], 25)
        self.assertEqual(workflow["18"]["inputs"]["noise_seed"], 42)
        self.assertEqual(workflow["24"]["class_type"], "SaveVideo")
        self.assertEqual(len(workflow), 19)
        self.assertNotIn("audio", workflow["23"]["inputs"])

    def test_output_path_uses_cineforge_namespace(self):
        workflow = ltx_workflow("frame.png", "move", "drift", 768, 432, 25, 42, 12)
        self.assertTrue(workflow["24"]["inputs"]["filename_prefix"].startswith("cineforge/"))

    def test_wan_workflow_routes_prompt_and_frame(self):
        workflow = wan22_workflow("motion-frame.png", "walks", "drift", 512, 288, 17, 9, True)
        self.assertEqual(workflow["97"]["inputs"]["image"], "motion-frame.png")
        self.assertEqual(workflow["129:93"]["inputs"]["text"], "walks")
        self.assertEqual(workflow["129:98"]["inputs"]["length"], 17)
        self.assertTrue(workflow["108"]["inputs"]["filename_prefix"].startswith("cineforge/"))


if __name__ == "__main__":
    unittest.main()
