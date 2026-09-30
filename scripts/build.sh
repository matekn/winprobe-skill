#!/usr/bin/env bash
# Build a self-contained single-file flaui.exe for Windows (win-x64).
# Works from Linux/macOS (cross-compile) or Windows (Git Bash/WSL).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
out="$root/dist"

echo "Publishing flaui to $out ..."
dotnet publish "$root/tool/flaui/flaui.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o "$out"

echo
echo "Built: $out/flaui.exe"
"$out/flaui.exe" --version || true
