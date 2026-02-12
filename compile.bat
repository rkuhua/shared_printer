@echo off
chcp 65001 >nul
setlocal

set "CSC="
if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if defined CSC goto Found

if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if defined CSC goto Found

echo [Error] C# Compiler (csc.exe) not found!
echo Please ensure .NET Framework 4.0 or higher is installed.
pause
exit /b 1

:Found
echo Found compiler at: %CSC%
echo Compiling PrinterManager...

"%CSC%" /codepage:65001 /target:winexe /out:一键添加共享打印机.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll PrinterManager.cs

if %errorlevel% equ 0 (
    echo.
    echo ==========================================
    echo Compilation Successful!
    echo Created: 一键添加共享打印机.exe
    echo ==========================================
    echo.
) else (
    echo.
    echo Compilation Failed!
    pause
)
