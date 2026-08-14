import unittest

from cineforge.planner import DELIMITER, build_plan


class PlannerTests(unittest.TestCase):
    def setUp(self):
        self.plan = build_plan({
            "title": "Test Sequence",
            "subject": "a courier",
            "environment": "a rain-dark station",
            "objective": "reach the train",
            "obstacle": "a watcher closes in",
            "action": "crosses the platform",
            "duration": "5",
            "continuity": "face, coat, case, rain, and screen direction",
        })

    def test_exact_branch_topology(self):
        self.assertEqual(set(self.plan["branches"]), {"angles", "inserts", "progression"})
        self.assertTrue(all(len(items) == 5 for items in self.plan["branches"].values()))
        self.assertEqual(sum(map(len, self.plan["branches"].values())), 15)

    def test_stable_ids_and_motion_prompts(self):
        ids = [shot["id"] for shots in self.plan["branches"].values() for shot in shots]
        self.assertEqual(len(ids), len(set(ids)))
        self.assertTrue(all("SUBJECT LOCK" in shot["motion_prompt"] for shots in self.plan["branches"].values() for shot in shots))

    def test_split_contract(self):
        self.assertEqual(self.plan["delimiter"], DELIMITER)
        self.assertFalse(any(DELIMITER in shot["prompt"] for shots in self.plan["branches"].values() for shot in shots))


if __name__ == "__main__":
    unittest.main()
