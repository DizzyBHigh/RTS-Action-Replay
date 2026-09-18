#!/usr/bin/env python3
import hashlib
import re
from pathlib import Path

OVERLAY = Path("overlay")
INDEX = OVERLAY / "index.html"
PATTERN = re.compile(r'((?:href|src)=")([^"]+?)(?:\?v=[^"]*)?(")')

text = INDEX.read_text(encoding="utf-8")

def versioned(match):
    prefix, url, suffix = match.groups()
    if url.startswith(("http://", "https://", "//", "#")):
        return match.group(0)

    path = (INDEX.parent / url.split("?")[0]).resolve()
    try:
        path.relative_to(OVERLAY.resolve())
    except ValueError:
        return match.group(0)

    if not path.is_file():
        return match.group(0)

    digest = hashlib.sha256(path.read_bytes()).hexdigest()[:12]
    clean_url = url.split("?")[0]
    return f"{prefix}{clean_url}?v={digest}{suffix}"

updated = PATTERN.sub(versioned, text)
INDEX.write_text(updated, encoding="utf-8")
