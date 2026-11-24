"""Author, donation card, feedback links and the guide (same values as the Windows AppInfo)."""

import platform
import subprocess
import urllib.parse

AUTHOR = "Bilolbek Yoldoshxojayev"
DONATION_CARD = "5614 6838 6100 6096"
REPO = "https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo"
GUIDE_URL = "https://bilolbekyoldoshxojayev.github.io/yangicha-alifbo/#qanday"
TEMPLATES = {"bug": "bug.yml", "word": "wrong-conversion.yml", "idea": "idea.yml"}


def feedback_url(kind, version, os_name):
    """New-issue form for a feedback kind, with only the version and OS prefilled."""
    query = urllib.parse.urlencode(
        {"template": TEMPLATES.get(kind, TEMPLATES["bug"]), "version": version, "os": os_name},
        quote_via=urllib.parse.quote)
    return f"{REPO}/issues/new?{query}"


def os_name():
    try:
        return platform.freedesktop_os_release().get("PRETTY_NAME") or "Linux"
    except (OSError, AttributeError):
        return "Linux"


def open_url(url):
    subprocess.Popen(["xdg-open", url], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
