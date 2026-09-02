@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo.
echo === More Guns: Il2Cpp ===
dotnet build "MoreGuns.csproj" -c Il2Cpp
if errorlevel 1 goto :fail

echo.
echo === More Guns: Mono ===
dotnet build "MoreGuns.csproj" -c Mono
if errorlevel 1 goto :fail

echo.
echo Build succeeded.
echo   Il2Cpp: "%~dp0bin\Il2Cpp\net6.0\MoreGuns.dll"
echo   Mono:   "%~dp0bin\Mono\net6.0\MoreGunsMono.dll"
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
