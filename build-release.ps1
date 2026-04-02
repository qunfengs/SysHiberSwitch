param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$releaseRoot = Join-Path $PSScriptRoot "release"
$packageRoot = Join-Path $releaseRoot ("SysHiberSwitch-v" + $Version)
$zipPath = $packageRoot + ".zip"

if (Test-Path $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

& (Join-Path $PSScriptRoot "build.ps1") -OutputPath (Join-Path $packageRoot "SysHiberSwitch.exe")

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.md") -Destination (Join-Path $packageRoot "README.md")

@"
SysHiberSwitch v$Version

1. 运行 SysHiberSwitch.exe
2. 点击“开启”可同时阻止息屏和休眠
3. 点击“关闭”可恢复系统电源计划
4. 点击“退出”可关闭程序
"@ | Set-Content -Path (Join-Path $packageRoot "使用说明.txt") -Encoding UTF8

Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath

Write-Host "Release package: $zipPath"
