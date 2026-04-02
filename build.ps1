param(
    [string]$OutputPath = ".\\bin\\SysHiberSwitch.exe"
)

$ErrorActionPreference = "Stop"

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) {
    throw "Compiler not found: $compiler"
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory -and -not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

& $compiler `
    /target:winexe `
    /nologo `
    /optimize+ `
    /out:$OutputPath `
    /r:System.dll `
    /r:System.Drawing.dll `
    /r:System.Windows.Forms.dll `
    .\Program.cs `
    .\AppState.cs `
    .\FloatingForm.cs

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $OutputPath"
