@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo.
echo  SenX KOTH Plugin Build
echo  ----------------------

set MSBUILD=
for %%v in (Enterprise Professional Community BuildTools) do (
    for /d %%d in ("C:\Program Files\Microsoft Visual Studio\2022\%%v\MSBuild\Current\Bin") do (
        if exist "%%~d\MSBuild.exe" set "MSBUILD=%%~d\MSBuild.exe"
    )
    if not "!MSBUILD!"=="" goto :found
)
echo [ERROR] Could not find MSBuild.exe.
exit /b 1

:found
echo   MSBuild: !MSBUILD!

if /i "%~1"=="Debug" (set CFG=Debug) else (set CFG=Release)
echo   Config:  !CFG!

set "SOLUTION_DIR=%~dp0"
if "!SOLUTION_DIR:~-1!"=="\" set "SOLUTION_DIR=!SOLUTION_DIR:~0,-1!"

echo.
"!MSBUILD!" "SenX KOTH Plugin\SenX KOTH Plugin.csproj" /p:Configuration=!CFG! /p:Platform=x64 /p:SolutionDir=!SOLUTION_DIR!\ /m /v:minimal

if errorlevel 1 (echo. & echo [FAILED] Build errors. Check output above. & exit /b 1)

if exist "Build\SenX KOTH Plugin.zip" (echo. & echo SUCCESS - Build\SenX KOTH Plugin.zip) else (echo. & echo [WARN] Zip not created. Check output.)
endlocal
