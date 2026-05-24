$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

function New-RoundedRectPath([System.Drawing.RectangleF]$rect, [float]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function Draw-Orbit($g, [System.Drawing.Color]$color, [float]$width, [float]$angle, [float]$rx, [float]$ry) {
    $state = $g.Save()
    $g.TranslateTransform(64, 64)
    $g.RotateTransform($angle)
    $pen = [System.Drawing.Pen]::new($color, $width)
    $g.DrawEllipse($pen, -$rx, -$ry, $rx * 2, $ry * 2)
    $pen.Dispose()
    $g.Restore($state)
}

function Draw-Particle($g, [float]$x, [float]$y, [float]$r, [System.Drawing.Color]$color) {
    $brush = [System.Drawing.SolidBrush]::new($color)
    $g.FillEllipse($brush, $x - $r, $y - $r, $r * 2, $r * 2)
    $brush.Dispose()
    $shine = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(120, 255, 255, 255))
    $g.FillEllipse($shine, $x - ($r / 2), $y - ($r / 2), $r * 0.65, $r * 0.65)
    $shine.Dispose()
}

function New-IconImage([int]$size) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.ScaleTransform($size / 128.0, $size / 128.0)

    $rect = [System.Drawing.Rectangle]::new(0, 0, 128, 128)
    $bg = [System.Drawing.Drawing2D.LinearGradientBrush]::new($rect, [System.Drawing.Color]::FromArgb(42, 26, 94), [System.Drawing.Color]::FromArgb(13, 8, 32), [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $path = New-RoundedRectPath ([System.Drawing.RectangleF]::new(0, 0, 128, 128)) 28
    $g.FillPath($bg, $path)
    $border = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(90, 138, 100, 255), 1.5)
    $g.DrawPath($border, $path)

    Draw-Orbit $g ([System.Drawing.Color]::FromArgb(150, 138, 100, 255)) 1.5 -30 42 16
    Draw-Orbit $g ([System.Drawing.Color]::FromArgb(95, 180, 140, 255)) 1.2 30 42 16
    Draw-Orbit $g ([System.Drawing.Color]::FromArgb(90, 108, 78, 255)) 1.2 90 42 14

    $halo = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(45, 109, 74, 255))
    $g.FillEllipse($halo, 46, 46, 36, 36)
    $core = [System.Drawing.Drawing2D.LinearGradientBrush]::new([System.Drawing.Rectangle]::new(53, 53, 22, 22), [System.Drawing.Color]::FromArgb(200, 168, 255), [System.Drawing.Color]::FromArgb(74, 45, 181), [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $g.FillEllipse($core, 53, 53, 22, 22)
    $shine = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(100, 255, 255, 255))
    $g.FillEllipse($shine, 56, 57, 8, 6)

    Draw-Particle $g 97 53 3.8 ([System.Drawing.Color]::FromArgb(220, 200, 168, 255))
    Draw-Particle $g 35 82 3.3 ([System.Drawing.Color]::FromArgb(205, 138, 100, 255))
    Draw-Particle $g 64 22 2.8 ([System.Drawing.Color]::FromArgb(190, 170, 136, 255))

    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)

    $writer.Write([UInt32]40)
    $writer.Write([Int32]$size)
    $writer.Write([Int32]($size * 2))
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]0)
    $writer.Write([UInt32]($size * $size * 4))
    $writer.Write([Int32]0)
    $writer.Write([Int32]0)
    $writer.Write([UInt32]0)
    $writer.Write([UInt32]0)

    for ($y = $size - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $size; $x++) {
            $pixel = $bitmap.GetPixel($x, $y)
            $writer.Write([byte]$pixel.B)
            $writer.Write([byte]$pixel.G)
            $writer.Write([byte]$pixel.R)
            $writer.Write([byte]$pixel.A)
        }
    }

    $maskStride = [int]([Math]::Ceiling($size / 32.0) * 4)
    $writer.Write((New-Object byte[] ($maskStride * $size)))
    $writer.Flush()
    $bytes = $stream.ToArray()

    $writer.Dispose()
    $shine.Dispose()
    $core.Dispose()
    $halo.Dispose()
    $border.Dispose()
    $bg.Dispose()
    $path.Dispose()
    $g.Dispose()
    $bitmap.Dispose()

    return ,$bytes
}

$sizes = @(256, 128, 64, 48, 32, 16)
$images = foreach ($size in $sizes) {
    [pscustomobject]@{
        Size = $size
        Bytes = New-IconImage $size
    }
}

$icoPath = Join-Path $assets 'quark.ico'
$fs = [System.IO.File]::Create($icoPath)
$writer = [System.IO.BinaryWriter]::new($fs)

$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$images.Count)

$offset = 6 + (16 * $images.Count)
foreach ($image in $images) {
    $writer.Write([byte]$(if ($image.Size -eq 256) { 0 } else { $image.Size }))
    $writer.Write([byte]$(if ($image.Size -eq 256) { 0 } else { $image.Size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$image.Bytes.Length)
    $writer.Write([UInt32]$offset)
    $offset += $image.Bytes.Length
}

foreach ($image in $images) {
    $writer.Write($image.Bytes)
}

$writer.Dispose()
$fs.Dispose()

Write-Host "Generated $icoPath"
