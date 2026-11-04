"""Stage the exact, verified binaries selected in tools.json."""
import argparse
import hashlib
import json
import shutil
import subprocess
import tarfile
import tempfile
import zipfile
from pathlib import Path


def copy_members(archive, names, destination):
    if archive.name.endswith(".zip"):
        with zipfile.ZipFile(archive) as package:
            for member in package.infolist():
                name = Path(member.filename).name
                if name in names and not member.is_dir():
                    with package.open(member) as source, (destination / name).open("wb") as target:
                        shutil.copyfileobj(source, target)
    else:
        with tarfile.open(archive, "r:xz") as package:
            for member in package:
                name = Path(member.name).name
                if name in names and member.isfile():
                    with package.extractfile(member) as source, (destination / name).open("wb") as target:
                        shutil.copyfileobj(source, target)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=["win-x64", "linux-x64"])
    parser.add_argument("output", type=Path)
    parser.add_argument("--cache", type=Path)
    args = parser.parse_args()
    suffix = ".exe" if args.platform == "win-x64" else ""
    manifest_path = Path(__file__).with_name("tools.json")
    manifest = json.loads(manifest_path.read_text())
    args.output.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory() as temporary:
        cache = args.cache or Path(temporary)
        cache.mkdir(parents=True, exist_ok=True)
        for tool, (url, digest) in manifest[args.platform].items():
            archive = cache / url.rsplit("/", 1)[-1]
            if not archive.is_file():
                temporary_file = archive.with_name(archive.name + ".partial")
                subprocess.run(["curl", "--fail", "--location", "--retry", "3",
                                "--connect-timeout", "20", "--silent", "--show-error",
                                "--output", str(temporary_file), url], check=True)
                temporary_file.replace(archive)
            with archive.open("rb") as downloaded:
                if hashlib.file_digest(downloaded, "sha256").hexdigest() != digest:
                    archive.unlink()
                    raise SystemExit(f"SHA-256 mismatch for {archive.name}")

            names = [tool + suffix]
            if tool == "ffmpeg":
                names.append("ffprobe" + suffix)
            if tool == "yt-dlp":
                shutil.copyfile(archive, args.output / names[0])
            else:
                copy_members(archive, names, args.output)
            for name in names:
                binary = args.output / name
                if not binary.is_file():
                    raise SystemExit(f"{name} was absent from {archive.name}")
                binary.chmod(0o755)
    shutil.copyfile(manifest_path, args.output / "versions.json")


if __name__ == "__main__":
    main()
