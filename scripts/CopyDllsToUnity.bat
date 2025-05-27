@echo off
REM Brigine DLL 手动拷贝脚本
REM 用途：手动执行DLL拷贝到Unity包
REM 作者：Brigine项目组

echo 手动执行Brigine DLL拷贝到Unity包...

REM 设置默认参数
set CONFIGURATION=Debug
set TARGET_FRAMEWORK=netstandard2.1

REM 如果提供了参数，使用提供的参数
if not "%1"=="" set CONFIGURATION=%1
if not "%2"=="" set TARGET_FRAMEWORK=%2

echo 配置: %CONFIGURATION%
echo 目标框架: %TARGET_FRAMEWORK%

REM 执行PowerShell脚本
powershell -ExecutionPolicy Bypass -File "%~dp0CopyDllsToUnity.ps1" -Configuration %CONFIGURATION% -TargetFramework %TARGET_FRAMEWORK%

if %ERRORLEVEL% EQU 0 (
    echo DLL拷贝完成！
) else (
    echo DLL拷贝失败，请检查错误信息
)

pause 