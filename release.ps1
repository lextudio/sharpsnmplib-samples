[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Configuration = 'Release'
)

# Improved OS detection
$isWindowsOS = ($PSVersionTable.PSVersion.Major -ge 6 -and $PSVersionTable.Platform -eq 'Win32NT') -or ($PSVersionTable.PSVersion.Major -lt 6 -and $env:OS -match 'Windows')
$isMacOSX = $PSVersionTable.PSVersion.Major -ge 6 -and $PSVersionTable.OS -match 'Darwin'
$isLinuxOS = $PSVersionTable.PSVersion.Major -ge 6 -and $PSVersionTable.OS -match 'Linux'

if ($isWindowsOS) {
    Write-Host "Detected Windows OS."
    
    Install-Module VSSetup -Scope CurrentUser -Force -ErrorAction SilentlyContinue
    Update-Module VSSetup -ErrorAction SilentlyContinue
    $instance = Get-VSSetupInstance -All -Prerelease -ErrorAction SilentlyContinue
    $installDir = $instance.installationPath
    
    if ($installDir) {
        Write-Host "Found VS in " + $installDir
        $msBuild = Join-Path -Path $installDir -ChildPath 'MSBuild\Current\Bin\MSBuild.exe'
        
        if (![System.IO.File]::Exists($msBuild)) {
            $msBuild = Join-Path -Path $installDir -ChildPath 'MSBuild\15.0\Bin\MSBuild.exe'
            
            if (![System.IO.File]::Exists($msBuild)) {
                Write-Host "MSBuild doesn't exist on Windows. Trying dotnet command..."
                $solution = "SharpSnmpLib.Samples.Windows.slnx"
                
                & dotnet restore $solution -c $Configuration
                & dotnet clean $solution -c $Configuration
                & dotnet build $solution -c $Configuration
            } else {
                Write-Host "Using MSBuild from VS2017."
                $solution = "SharpSnmpLib.Samples.Windows.slnx"
                
                & $msBuild $solution /p:Configuration=$Configuration /t:restore
                & $msBuild $solution /p:Configuration=$Configuration /t:clean
                & $msBuild $solution /p:Configuration=$Configuration
            }
        } else {
            Write-Host "Using MSBuild from VS2019 or newer."
            $solution = "SharpSnmpLib.Samples.Windows.slnx"
            
            & $msBuild $solution /p:Configuration=$Configuration /t:restore
            & $msBuild $solution /p:Configuration=$Configuration /t:clean
            & $msBuild $solution /p:Configuration=$Configuration
        }
    } else {
        Write-Host "Visual Studio not found. Using dotnet command..."
        $solution = "SharpSnmpLib.Samples.Windows.slnx"
        
        & dotnet restore $solution -c $Configuration
        & dotnet clean $solution -c $Configuration
        & dotnet build $solution -c $Configuration
    }
} else {
    # On macOS or Linux
    if ($isMacOSX) {
        Write-Host "Detected macOS."
    } elseif ($isLinuxOS) {
        Write-Host "Detected Linux."
    } else {
        Write-Host "Detected non-Windows OS (unknown type)."
    }
    
    $solution = "SharpSnmpLib.Samples.slnx"
    Write-Host "Using cross-platform solution: $solution"
    
    & dotnet restore $solution -c $Configuration
    & dotnet clean $solution -c $Configuration
    & dotnet build $solution -c $Configuration
}

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Compilation failed. Exit."
    exit $LASTEXITCODE
}

Write-Host "Compilation finished."
