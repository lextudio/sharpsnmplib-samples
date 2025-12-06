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
    Write-Host "Detected Windows OS. Use VSWhere instead."

    $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe -products * -nologo | select-object -first 1
    if (![System.IO.File]::Exists($msBuild))
    {
        Write-Host "MSBuild doesn't exist. Exit."
        exit 1
    }

    $solution = "SharpSnmpLib.Samples.Windows.slnx"
    & $msBuild $solution /p:Configuration=$Configuration /t:restore
    & $msBuild $solution /p:Configuration=$Configuration /t:clean
    & $msBuild $solution /p:Configuration=$Configuration
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
