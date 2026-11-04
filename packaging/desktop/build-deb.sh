#!/usr/bin/env bash
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
publish="${1:?Pass the published application directory}"
destination="${2:?Pass the output .deb path}"
version="${3:-1.1.0}"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
app="$stage/opt/ramin-downloader"

install -d "$app" "$stage/DEBIAN" "$stage/usr/bin" "$stage/usr/share/applications" \
  "$stage/usr/share/icons/hicolor/scalable/apps" "$stage/usr/share/doc/ramin-downloader"
cp -a "$publish/." "$app/"
for binary in yt-dlp ffmpeg ffprobe deno; do
  test -x "$app/tools/$binary"
done
ln -s /opt/ramin-downloader/RaminDownloader.Desktop "$stage/usr/bin/ramin-downloader"
install -m644 "$here/ramin-downloader.desktop" "$stage/usr/share/applications/"
install -m644 "$here/ramin-downloader.svg" "$stage/usr/share/icons/hicolor/scalable/apps/"
install -m644 "$repo/LICENSE" "$stage/usr/share/doc/ramin-downloader/copyright"
install -m644 "$here/NOTICES.md" "$stage/usr/share/doc/ramin-downloader/"
cat > "$stage/DEBIAN/control" <<CONTROL
Package: ramin-downloader
Version: $version
Architecture: amd64
Section: video
Priority: optional
Maintainer: RaminDownloader maintainers
Depends: libc6 (>= 2.35), libgcc-s1, libstdc++6, libssl3t64 | libssl3, libicu78 | libicu77 | libicu76 | libicu74 | libicu72 | libicu70, libx11-6, libice6, libsm6, libfontconfig1, libxext6, libxrender1, libxi6, libxrandr2, libxcursor1, zlib1g, xdg-utils
Description: Desktop media downloader and converter
 Download web media and resize, compress or convert local files.
CONTROL

mkdir -p "$(dirname "$destination")"
dpkg-deb -Zxz -z3 --root-owner-group --build "$stage" "$destination"
