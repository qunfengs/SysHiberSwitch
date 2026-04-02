$ErrorActionPreference = "Stop"

$assetsDirectory = Join-Path $PSScriptRoot "assets"
$iconPath = Join-Path $assetsDirectory "app.ico"

if (-not (Test-Path $assetsDirectory)) {
    New-Item -ItemType Directory -Path $assetsDirectory | Out-Null
}

Add-Type -AssemblyName System.Drawing

$bitmap = New-Object System.Drawing.Bitmap 64, 64
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$backgroundRect = New-Object System.Drawing.Rectangle 2, 2, 60, 60
$backgroundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point 0, 0),
    (New-Object System.Drawing.Point 64, 64),
    [System.Drawing.Color]::FromArgb(70, 170, 120),
    [System.Drawing.Color]::FromArgb(36, 120, 210)
)
$graphics.FillEllipse($backgroundBrush, $backgroundRect)

$centerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 248, 252))
$graphics.FillEllipse($centerBrush, 17, 17, 30, 30)

$tickPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(36, 120, 210), 5)
$graphics.DrawLine($tickPen, 24, 33, 30, 40)
$graphics.DrawLine($tickPen, 30, 40, 41, 25)

$memoryStream = New-Object System.IO.MemoryStream
$bitmap.Save($memoryStream, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $memoryStream.ToArray()

$iconStream = New-Object System.IO.FileStream($iconPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$writer = New-Object System.IO.BinaryWriter($iconStream)

$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]64)
$writer.Write([Byte]64)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$pngBytes.Length)
$writer.Write([UInt32]22)
$writer.Write($pngBytes)

$writer.Dispose()
$iconStream.Dispose()
$memoryStream.Dispose()
$tickPen.Dispose()
$centerBrush.Dispose()
$backgroundBrush.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
