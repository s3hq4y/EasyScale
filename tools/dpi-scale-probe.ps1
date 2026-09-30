<#
.SYNOPSIS
  试验 Windows 每显示器缩放（DPI Scale）的读取与修改。
.DESCRIPTION
  调用未公开接口 DisplayConfigGetDeviceInfo / SetDeviceInfo (type = -3 / -4)。
  DPI 缩放是「源(source)」的属性，故 header.id 填 sourceId。
  仅供试验：改档后可用 -Restore 自动还原。
.EXAMPLE
  pwsh -File .\dpi-scale-probe.ps1 -List
.EXAMPLE
  pwsh -File .\dpi-scale-probe.ps1 -Delta 1 -Restore
.EXAMPLE
  pwsh -File .\dpi-scale-probe.ps1 -SetRel 0
#>
[CmdletBinding()]
param(
  [switch]$List,
  [int]$Delta = 0,
  [int]$SetRel = [int]::MinValue,
  [switch]$Restore,
  [int]$SourceIndex = 0,
  [int]$DelaySeconds = 3
)

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class EasyScaleNative {
  [DllImport("user32.dll")] public static extern int GetDisplayConfigBufferSizes(uint flags, out uint np, out uint nm);
  [DllImport("user32.dll")] public static extern int QueryDisplayConfig(uint flags, ref uint np, IntPtr paths, ref uint nm, IntPtr modes, IntPtr topo);
  [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(IntPtr p);
  [DllImport("user32.dll")] public static extern int DisplayConfigSetDeviceInfo(IntPtr p);
  [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
  [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint x, out uint y);
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
}
"@

# DISPLAYCONFIG_PATH_INFO = source(20) + target(48) + flags(4) = 72
$PATH_SIZE = 72
$QDC_ONLY_ACTIVE_PATHS = 2

function Get-ActiveSources {
  $np = 0; $nm = 0
  $rc = [EasyScaleNative]::GetDisplayConfigBufferSizes($QDC_ONLY_ACTIVE_PATHS, [ref]$np, [ref]$nm)
  if ($rc -ne 0) { throw "GetDisplayConfigBufferSizes rc=$rc" }
  $pBuf = [Runtime.InteropServices.Marshal]::AllocHGlobal($PATH_SIZE * [int]$np)
  $mBuf = [Runtime.InteropServices.Marshal]::AllocHGlobal(64 * [int]$nm)
  try {
    $rc = [EasyScaleNative]::QueryDisplayConfig($QDC_ONLY_ACTIVE_PATHS, [ref]$np, $pBuf, [ref]$nm, $mBuf, [IntPtr]::Zero)
    if ($rc -ne 0) { throw "QueryDisplayConfig rc=$rc" }
    $out = @()
    for ($i = 0; $i -lt $np; $i++) {
      $b = [IntPtr]::Add($pBuf, $i * $PATH_SIZE)
      $out += [pscustomobject]@{
        AdapterLo = [Runtime.InteropServices.Marshal]::ReadInt32($b, 0)
        AdapterHi = [Runtime.InteropServices.Marshal]::ReadInt32($b, 4)
        SourceId  = [Runtime.InteropServices.Marshal]::ReadInt32($b, 8)
      }
    }
    return $out
  } finally {
    [Runtime.InteropServices.Marshal]::FreeHGlobal($pBuf)
    [Runtime.InteropServices.Marshal]::FreeHGlobal($mBuf)
  }
}

function Get-DpiScaleRel {
  param($Source)
  $b = [Runtime.InteropServices.Marshal]::AllocHGlobal(32)
  try {
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 0, -3)   # GET_DPI_SCALE
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 4, 32)   # header.size
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 8, $Source.AdapterLo)
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 12, $Source.AdapterHi)
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 16, $Source.SourceId)
    $rc = [EasyScaleNative]::DisplayConfigGetDeviceInfo($b)
    if ($rc -ne 0) { throw "GET_DPI_SCALE rc=$rc" }
    return [pscustomobject]@{
      Min = [Runtime.InteropServices.Marshal]::ReadInt32($b, 20)
      Cur = [Runtime.InteropServices.Marshal]::ReadInt32($b, 24)
      Max = [Runtime.InteropServices.Marshal]::ReadInt32($b, 28)
    }
  } finally { [Runtime.InteropServices.Marshal]::FreeHGlobal($b) }
}

function Set-DpiScaleRel {
  param($Source, [int]$Rel)
  $b = [Runtime.InteropServices.Marshal]::AllocHGlobal(24)
  try {
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 0, -4)   # SET_DPI_SCALE
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 4, 24)   # header.size
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 8, $Source.AdapterLo)
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 12, $Source.AdapterHi)
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 16, $Source.SourceId)
    [Runtime.InteropServices.Marshal]::WriteInt32($b, 20, $Rel)
    $rc = [EasyScaleNative]::DisplayConfigSetDeviceInfo($b)
    if ($rc -ne 0) { throw "SET_DPI_SCALE rc=$rc" }
  } finally { [Runtime.InteropServices.Marshal]::FreeHGlobal($b) }
}

function Get-PrimaryDpi {
  $pt = [EasyScaleNative+POINT]::new()
  $pt.X = 0; $pt.Y = 0
  $h = [EasyScaleNative]::MonitorFromPoint($pt, 2)  # MONITOR_DEFAULTTONEAREST
  [uint32]$x = 0; [uint32]$y = 0
  $rc = [EasyScaleNative]::GetDpiForMonitor($h, 0, [ref]$x, [ref]$y)  # MDT_EFFECTIVE_DPI
  if ($rc -ne 0) { return $null }
  return $x
}

$sources = @(Get-ActiveSources)
if ($sources.Count -eq 0) { throw "未找到活动的显示源" }

Write-Host "活动显示源：" -ForegroundColor Cyan
for ($i = 0; $i -lt $sources.Count; $i++) {
  $sc = Get-DpiScaleRel -Source $sources[$i]
  Write-Host ("  [{0}] sourceId={1,-3}  档位: min={2} cur={3} max={4}" -f `
    $i, $sources[$i].SourceId, $sc.Min, $sc.Cur, $sc.Max)
}

$noAction = $List -or ($Delta -eq 0 -and $SetRel -eq [int]::MinValue)
if ($noAction) {
  $dpi = Get-PrimaryDpi
  if ($dpi) { Write-Host ("`n主显示器当前 DPI = {0}  ({1}%)" -f $dpi, [math]::Round($dpi / 96 * 100)) }
  Write-Host "`n用法：" -ForegroundColor Yellow
  Write-Host "  -Delta 1 -Restore     相对 +1 档，自动还原"
  Write-Host "  -SetRel 0             绝对设为 0 档"
  Write-Host "  -SourceIndex 1        指定第 2 个显示源"
  return
}

if ($SourceIndex -lt 0 -or $SourceIndex -ge $sources.Count) { throw "SourceIndex 超出范围" }
$src = $sources[$SourceIndex]
$before = Get-DpiScaleRel -Source $src
$target = if ($SetRel -ne [int]::MinValue) { $SetRel } else { $before.Cur + $Delta }

if ($target -lt $before.Min -or $target -gt $before.Max) {
  throw "目标档位 $target 超出该显示源范围 [$($before.Min), $($before.Max)]"
}

Write-Host ("`n修改: cur {0} -> {1}" -f $before.Cur, $target) -ForegroundColor Green
Set-DpiScaleRel -Source $src -Rel $target
Start-Sleep -Milliseconds 800

$after = Get-DpiScaleRel -Source $src
$dpi = Get-PrimaryDpi
Write-Host ("读回: cur={0}" -f $after.Cur)
if ($dpi) { Write-Host ("DPI = {0}  ({1}%)" -f $dpi, [math]::Round($dpi / 96 * 100)) }

if ($Restore) {
  Write-Host ("`n{0} 秒后还原为 {1} ..." -f $DelaySeconds, $before.Cur) -ForegroundColor Yellow
  Start-Sleep -Seconds $DelaySeconds
  Set-DpiScaleRel -Source $src -Rel $before.Cur
  Start-Sleep -Milliseconds 800
  $back = Get-DpiScaleRel -Source $src
  Write-Host ("已还原: cur={0}" -f $back.Cur) -ForegroundColor Green
}
