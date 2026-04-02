param(
    [string]$OutputPath = ".\\bin\\SysHiberSwitch.exe"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) {
    throw "Compiler not found: $compiler"
}

$iconScript = Join-Path $scriptRoot "generate-icon.ps1"
if (Test-Path $iconScript) {
    & $iconScript
}

$outputPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputPath)) {
    $outputPath = Join-Path $scriptRoot $outputPath
}

$outputPath = [System.IO.Path]::GetFullPath($outputPath)

$outputDirectory = Split-Path -Parent $outputPath
if ($outputDirectory -and -not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

& $compiler `
    /target:winexe `
    /nologo `
    /optimize+ `
    /win32icon:"$scriptRoot\assets\app.ico" `
    /out:$outputPath `
    /r:System.dll `
    /r:System.Drawing.dll `
    /r:System.Windows.Forms.dll `
    "$scriptRoot\Program.cs" `
    "$scriptRoot\AppState.cs" `
    "$scriptRoot\FloatingForm.cs" `
    "$scriptRoot\AssemblyInfo.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $outputPath"
