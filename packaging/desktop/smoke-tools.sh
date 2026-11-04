#!/usr/bin/env bash
set -euo pipefail
tools="$(cd "${1:?Pass the bundled tool directory}" && pwd)"
suffix=""
if [[ -x "$tools/ffmpeg.exe" ]]; then suffix=".exe"; fi
temp="$(mktemp -d)"
trap 'rm -rf "$temp"' EXIT
cd "$temp"

"$tools/yt-dlp$suffix" --ignore-config --version
"$tools/deno$suffix" --version
ffmpeg="$tools/ffmpeg$suffix"
ffprobe="$tools/ffprobe$suffix"
"$ffmpeg" -v error -f lavfi -i testsrc2=size=320x240:rate=10 -f lavfi -i sine=440 \
  -t 1 -c:v libx264 -c:a aac sample.mp4
"$ffmpeg" -v error -i sample.mp4 -vf scale=-2:120 -c:v libx264 -crf 30 resized.mp4
"$ffmpeg" -v error -i sample.mp4 -c:v libvpx-vp9 -c:a libopus video.webm
"$ffmpeg" -v error -i sample.mp4 -vn -c:a libmp3lame audio.mp3
test "$("$ffprobe" -v error -select_streams v:0 -show_entries stream=height -of csv=p=0 resized.mp4 | tr -d '\r')" = "120"
test "$("$ffprobe" -v error -show_entries stream=codec_name -of csv=p=0 audio.mp3 | tr -d '\r')" = "mp3"
