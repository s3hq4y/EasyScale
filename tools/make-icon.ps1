<#
.SYNOPSIS
  生成 EasyScale 的托盘/应用图标（Assets/easy-scale.ico）。
.DESCRIPTION
  纯脚本生成，避免把二进制图标硬编码进仓库。
  图案：圆角方块背景 + 两条对角线箭头，象征「放大/缩放」。
.EXAMPLE
  pwsh -File .\tools\make-icon.ps1
#>
[CmdletBinding()]
param(
  [string]$OutputPath = (Join-Path $PSScriptRoot '..\src\EasyScale.App\Assets\easy-scale.ico'),
  [int]$Size = 256
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$s) {
  $bmp = New-Object System.Drawing.Bitmap($s, $s)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.Clear([System.Drawing.Color]::Transparent)

  # 背景圆角方块（品牌蓝）
  $bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 120, 212))
  $r = [int]($s * 0.18)
  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
  $path.AddArc($s - $r * 2, 0, $r * 2, $r * 2, 270, 90)
  $path.AddArc($s - $r * 2, $s - $r * 2, $r * 2, $r * 2, 0, 90)
  $path.AddArc(0, $s - $r * 2, $r * 2, $r * 2, 90, 90)
  $path.CloseFigure()
  $g.FillPath($bg, $path)

  # 两条对角线箭头（白色）
  $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [float]($s * 0.08))
  $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
  $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
  $m = [int]($s * 0.28)
  $g.DrawLine($pen, $m, $s - $m, $s - $m, $m)          # 主对角线
  $g.DrawLine($pen, $s - $m, $m, $s - $m - $r, $m)      # 右上箭头横
  $g.DrawLine($pen, $s - $m, $m, $s - $m, $m + $r)      # 右上箭头竖

  $g.Dispose()
  return $bmp
}

# 生成多尺寸，打包为 ICO。
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @()
foreach ($sz in $sizes) { $images += ,(New-IconBitmap $sz) }

$dir = Split-Path -Parent $OutputPath
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

# 手写 ICO 容器：header(6) + directory entries + PNG 数据
$pngData = @()
foreach ($img in $images) {
  $ms = New-Object System.IO.MemoryStream
  $img.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  $pngData += ,$ms.ToArray()
  $ms.Dispose()
  $img.Dispose()
}

$fs = [System.IO.File]::Create($OutputPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)                 # reserved
$bw.Write([uint16]1)                 # type = icon
$bw.Write([uint16]$sizes.Count)      # count

$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $sz = $sizes[$i]
  $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))  # width
  $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))  # height
  $bw.Write([byte]0)                 # colors
  $bw.Write([byte]0)                 # reserved
  $bw.Write([uint16]1)               # planes
  $bw.Write([uint16]32)              # bpp
  $bw.Write([uint32]$pngData[$i].Length)
  $bw.Write([uint32]$offset)
  $offset += $pngData[$i].Length
}
foreach ($data in $pngData) { $bw.Write($data) }

$bw.Flush(); $bw.Close(); $fs.Close()
Write-Host "Icon written: $OutputPath ($((Get-Item $OutputPath).Length) bytes)"
