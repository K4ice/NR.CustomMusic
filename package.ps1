# Собирает Release и упаковывает zip для GitHub Releases / Nexus.
# Запуск: powershell -ExecutionPolicy Bypass -File package.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$version = ([xml](Get-Content 'NR.CustomMusic.csproj')).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
dotnet build -c Release -p:SkipCopy=true -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$stage = Join-Path 'dist' 'stage'
Remove-Item 'dist' -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force "$stage\BepInEx\plugins\NR.CustomMusic", "$stage\CustomMusic" | Out-Null

Copy-Item 'bin\Release\NR.CustomMusic.dll' "$stage\BepInEx\plugins\NR.CustomMusic\"
Copy-Item 'README.md', 'LICENSE' "$stage\BepInEx\plugins\NR.CustomMusic\"
Set-Content "$stage\CustomMusic\PUT_YOUR_MUSIC_HERE.txt" -Encoding utf8 -Value @'
Put your .mp3 / .ogg / .wav files into this folder.
Кидай сюда свои .mp3 / .ogg / .wav файлы.
'@

$zip = Join-Path $PSScriptRoot "dist\NR.CustomMusic-$version.zip"
# Compress-Archive в Windows PowerShell пишет пути через '\', это ломает Vortex и часть архиваторов.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = (Resolve-Path $stage).Path
$archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    Get-ChildItem $root -Recurse -File | ForEach-Object {
        $entry = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry) | Out-Null
    }
} finally { $archive.Dispose() }
Remove-Item $stage -Recurse -Force
Write-Host "Done: $zip"
