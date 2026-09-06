@echo off
setlocal EnableExtensions
cd /d "%~dp0"

rem =============================================================================
rem  EDIT THESE
rem =============================================================================

set "VERSION=1.6.4"

rem Folder where finished zips are written (NOT used as the build output)
set "ZIP_OUT_DIR=%~dp0..\Schedule I"

rem Where DLLs are built (forced — never the zip folder)
set "IL2CPP_OUT=%~dp0bin\Il2Cpp\net6.0"
set "MONO_OUT=%~dp0bin\Mono\net6.0"

set "IL2CPP_DLL=%IL2CPP_OUT%\MoreGuns.dll"
set "MONO_DLL=%MONO_OUT%\MoreGunsMono.dll"

rem Zip file names only (paths come from ZIP_OUT_DIR)
set "ZIP_IL2CPP_NAME=More Guns Forked-%VERSION% IL2CPP.zip"
set "ZIP_MONO_NAME=More Guns Forked-%VERSION% MONO.zip"

rem =============================================================================
rem  BUILD  ( -o locks output under bin\ so nothing spills into ZIP_OUT_DIR )
rem =============================================================================

set "ZIP_IL2CPP=%ZIP_OUT_DIR%\%ZIP_IL2CPP_NAME%"
set "ZIP_MONO=%ZIP_OUT_DIR%\%ZIP_MONO_NAME%"

echo.
echo === More Guns: Il2Cpp ===
dotnet build "MoreGuns.csproj" -c Il2Cpp -o "%IL2CPP_OUT%"
if errorlevel 1 goto :fail

echo.
echo === More Guns: Mono ===
dotnet build "MoreGuns.csproj" -c Mono -o "%MONO_OUT%"
if errorlevel 1 goto :fail

rem =============================================================================
rem  PACKAGE  (stage under %%TEMP%%, then zip — never copies loose DLLs to ZIP_OUT_DIR)
rem =============================================================================

echo.
echo === Packaging Nexus zips ===
if not exist "%ZIP_OUT_DIR%" mkdir "%ZIP_OUT_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "Add-Type -AssemblyName System.IO.Compression.FileSystem;" ^
  "function Pack([string]$dll,[string]$zip){" ^
  "  if(-not (Test-Path -LiteralPath $dll)){ throw \"Missing build output: $dll\" };" ^
  "  $stage=Join-Path $env:TEMP ('mg-pack-'+[guid]::NewGuid().ToString('N'));" ^
  "  $mods=Join-Path $stage 'Mods';" ^
  "  New-Item -ItemType Directory -Path $mods -Force | Out-Null;" ^
  "  Copy-Item -LiteralPath $dll -Destination (Join-Path $mods (Split-Path $dll -Leaf)) -Force;" ^
  "  if(Test-Path -LiteralPath $zip){ Remove-Item -LiteralPath $zip -Force };" ^
  "  [IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip);" ^
  "  Remove-Item -LiteralPath $stage -Recurse -Force;" ^
  "  Write-Host ('  Created '+(Split-Path $zip -Leaf)+'  ('+((Get-Item -LiteralPath $zip).Length)+' bytes)')" ^
  "};" ^
  "Pack '%IL2CPP_DLL%' '%ZIP_IL2CPP%';" ^
  "Pack '%MONO_DLL%' '%ZIP_MONO%'"
if errorlevel 1 goto :fail

echo.
echo Build succeeded.
echo   Il2Cpp DLL:  "%IL2CPP_DLL%"
echo   Mono DLL:    "%MONO_DLL%"
echo   Il2Cpp zip:  "%ZIP_IL2CPP%"
echo   Mono zip:    "%ZIP_MONO%"
echo.
if /i "%~1"=="nopause" exit /b 0
pause
exit /b 0

:fail
echo.
echo Build failed.
if /i "%~1"=="nopause" exit /b 1
pause
exit /b 1
