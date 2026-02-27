[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Configuration = 'Release'
)

$solution = "SharpSnmpLib.Samples.slnx"
Write-Host "Using cross-platform solution: $solution"

& dotnet restore $solution -c $Configuration
& dotnet clean $solution -c $Configuration
& dotnet build $solution -c $Configuration

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Compilation failed. Exit."
    exit $LASTEXITCODE
}

Write-Host "Compilation finished."
