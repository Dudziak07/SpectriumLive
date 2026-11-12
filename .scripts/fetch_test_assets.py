"""Downloads local real-world test image assets (Kodak suite + high-res public domain images) into .test-assets/."""

from pathlib import Path
import sys
import urllib.error
import urllib.request

ROOT_DIR = Path(__file__).resolve().parent.parent
ASSETS_DIR = ROOT_DIR / ".test-assets"
KODAK_DIR = ASSETS_DIR / "kodak"
HIGHRES_DIR = ASSETS_DIR / "highres"

USER_AGENT = "SpectriumLiveTestFetcher/1.0 (https://github.com/Dudziak07/SpectriumLive)"

KODAK_BASE_URLS = [
    "https://raw.githubusercontent.com/lemire/kodakimagecollection/master",
    "https://r0k.us/graphics/kodak/kodak",
]

HIGHRES_IMAGES = {
    "apollo17_blue_marble.jpg": [
        "https://upload.wikimedia.org/wikipedia/commons/7/70/The_Blue_Marble%2C_AS17-148-22727.jpg",
    ],
    "pillars_of_creation_hst.jpg": [
        "https://upload.wikimedia.org/wikipedia/commons/8/81/Pillars_of_creation_2014_HST_WFC3-UVIS_full-res.jpg",
    ],
    "grand_canyon_landscape.jpg": [
        "https://upload.wikimedia.org/wikipedia/commons/0/03/Grand_Canyon_NP-Arizona-USA.jpg",
    ],
}


def download_file(urls: list[str], destination: Path) -> bool:
    if destination.exists() and destination.stat().st_size > 1024:
        print(f"SKIP (exists): {destination.name} ({destination.stat().st_size:,} bytes)")
        return True

    for url in urls:
        req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                if resp.status != 200:
                    print(f"WARN: HTTP {resp.status} for {url}")
                    continue
                data = resp.read()
                if len(data) < 1024:
                    print(f"WARN: Too small ({len(data)} bytes) for {url}")
                    continue
                destination.write_bytes(data)
                print(f"OK: {destination.name} ({len(data):,} bytes)")
                return True
        except (urllib.error.URLError, TimeoutError, OSError) as exc:
            print(f"WARN: Failed {url}: {exc}")

    return False


def main() -> int:
    KODAK_DIR.mkdir(parents=True, exist_ok=True)
    HIGHRES_DIR.mkdir(parents=True, exist_ok=True)

    failed = 0

    print("=== Downloading Kodak Lossless True Color Image Suite (24 PNGs) ===")
    for i in range(1, 25):
        filename = f"kodim{i:02d}.png"
        urls = [f"{base}/{filename}" for base in KODAK_BASE_URLS]
        if not download_file(urls, KODAK_DIR / filename):
            failed += 1

    print("\n=== Downloading 3 High-Resolution Public Domain Photos ===")
    for filename, urls in HIGHRES_IMAGES.items():
        if not download_file(urls, HIGHRES_DIR / filename):
            failed += 1

    if failed > 0:
        print(f"\nCompleted with {failed} failure(s).", file=sys.stderr)
        return 1

    print("\nAll test assets downloaded successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

