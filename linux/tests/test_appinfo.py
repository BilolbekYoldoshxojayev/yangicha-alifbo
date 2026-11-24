"""Feedback links and issue forms used by the IBus menu."""

import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "engine"))

import appinfo  # noqa: E402

REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))


class AppInfoTests(unittest.TestCase):
    def test_feedback_url_matches_windows(self):
        url = appinfo.feedback_url("word", "2.3.0", "Ubuntu 24.04 LTS")
        self.assertEqual(url, appinfo.REPO + "/issues/new?template=wrong-conversion.yml&version=2.3.0&os=Ubuntu%2024.04%20LTS")

    def test_unknown_kind_falls_back_to_bug(self):
        self.assertIn("template=bug.yml", appinfo.feedback_url("x", "1", "L"))

    def test_query_characters_are_escaped(self):
        url = appinfo.feedback_url("bug", "1&x=1", "a#b")
        self.assertIn("version=1%26x%3D1", url)
        self.assertIn("os=a%23b", url)

    def test_issue_forms_exist(self):
        for name in appinfo.TEMPLATES.values():
            path = os.path.join(REPO_ROOT, ".github", "ISSUE_TEMPLATE", name)
            self.assertTrue(os.path.isfile(path), name)

    def test_card_has_16_digits(self):
        self.assertEqual(sum(c.isdigit() for c in appinfo.DONATION_CARD), 16)


if __name__ == "__main__":
    unittest.main()
