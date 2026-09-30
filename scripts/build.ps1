# Build a self-contained single-file flaui.exe for Windows (win-x64).
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "dist"
$proj = Join-Path $root "tool\flaui\flaui.csproj"

Write-Host "Publishing flaui to $out ..."
dotnet publish $proj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o $out

Write-Host ""
Write-Host "Built: $out\flaui.exe"
& (Join-Path $out "flaui.exe") --version
