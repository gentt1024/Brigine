param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Debug",
    
    [Parameter(Mandatory=$false)]
    [string]$TargetFramework = "netstandard2.1"
)

$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectRoot = Split-Path -Parent $ScriptPath
$SourceBasePath = Join-Path $ProjectRoot "src"
$UnityPackagePath = Join-Path $ProjectRoot "engine_packages\com.brigine.unity\Runtime"

Write-Host "Starting DLL copy to Unity package..." -ForegroundColor Green
Write-Host "Project root: $ProjectRoot" -ForegroundColor Cyan
Write-Host "Unity package path: $UnityPackagePath" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration, Framework: $TargetFramework" -ForegroundColor Cyan

if (-not (Test-Path $UnityPackagePath)) {
    Write-Error "Unity package path does not exist: $UnityPackagePath"
    exit 1
}

$ProjectsToCopy = @(
    @{
        Name = "Brigine.Core"
        Files = @("Brigine.Core.dll", "Brigine.Core.pdb")
    },
    @{
        Name = "Brigine.Communication.Client"
        Files = @("Brigine.Communication.Client.dll", "Brigine.Communication.Client.pdb")
    },
    @{
        Name = "Brigine.Communication.Protos"
        Files = @("Brigine.Communication.Protos.dll", "Brigine.Communication.Protos.pdb")
    },
    @{
        Name = "Brigine.USD"
        Files = @("Brigine.USD.dll", "Brigine.USD.pdb")
    }
)

$UsdNetFiles = @("USD.NET.dll")
$TotalCopied = 0
$TotalErrors = 0

foreach ($Project in $ProjectsToCopy) {
    $ProjectName = $Project.Name
    $SourcePath = Join-Path $SourceBasePath "$ProjectName\bin\$Configuration\$TargetFramework"
    
    Write-Host "`nProcessing project: $ProjectName" -ForegroundColor Yellow
    Write-Host "   Source path: $SourcePath" -ForegroundColor Gray
    
    if (-not (Test-Path $SourcePath)) {
        Write-Warning "Source path does not exist, skipping: $SourcePath"
        continue
    }
    
    foreach ($FileName in $Project.Files) {
        $SourceFile = Join-Path $SourcePath $FileName
        $DestFile = Join-Path $UnityPackagePath $FileName
        
        if (Test-Path $SourceFile) {
            try {
                Copy-Item $SourceFile $DestFile -Force
                Write-Host "   Copied: $FileName" -ForegroundColor Green
                $TotalCopied++
            }
            catch {
                Write-Error "   Copy failed: $FileName - $($_.Exception.Message)"
                $TotalErrors++
            }
        }
        else {
            Write-Warning "   File not found: $FileName"
        }
    }
}

Write-Host "`nProcessing USD.NET dependencies" -ForegroundColor Yellow

$UsdNetSourcePaths = @(
    (Join-Path $SourceBasePath "Brigine.Core\bin\$Configuration\$TargetFramework"),
    (Join-Path $SourceBasePath "Brigine.USD\bin\$Configuration\$TargetFramework")
)

foreach ($UsdNetFile in $UsdNetFiles) {
    $Found = $false
    
    foreach ($SearchPath in $UsdNetSourcePaths) {
        $SourceFile = Join-Path $SearchPath $UsdNetFile
        
        if (Test-Path $SourceFile) {
            $DestFile = Join-Path $UnityPackagePath $UsdNetFile
            
            try {
                Copy-Item $SourceFile $DestFile -Force
                Write-Host "   Copied: $UsdNetFile (from $SearchPath)" -ForegroundColor Green
                $TotalCopied++
                $Found = $true
                break
            }
            catch {
                Write-Error "   Copy failed: $UsdNetFile - $($_.Exception.Message)"
                $TotalErrors++
            }
        }
    }
    
    if (-not $Found) {
        Write-Warning "   USD.NET file not found: $UsdNetFile"
    }
}

Write-Host "`nCopy summary:" -ForegroundColor Magenta
Write-Host "   Successfully copied: $TotalCopied files" -ForegroundColor Green
if ($TotalErrors -gt 0) {
    Write-Host "   Copy failed: $TotalErrors files" -ForegroundColor Red
}

Write-Host "`nUnity package now contains the latest Brigine DLL files!" -ForegroundColor Green
Write-Host "Tip: Please refresh the project in Unity to load the new DLL files" -ForegroundColor Cyan

if ($TotalErrors -gt 0) {
    exit 1
}
else {
    exit 0
} 