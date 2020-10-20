$output = dotnet --info | grep "Base Path:"
Write-Host $output

$comma = $output.LastIndexOf(':')
$path = $output.SubString($comma + 1).Trim()
Write-Host $path

$winfx = "$path/Sdks/Microsoft.NET.Sdk.WindowsDesktop/targets/Microsoft.WinFX.props"
if (-not (Test-Path $winfx)) {
    Write-Host "Didn't find $winfx"
    $winfx2 = "$path/Sdks/Microsoft.NET.Sdk.WindowsDesktop/targets/Microsoft.WinFx.props"
    if (Test-Path $winfx2) {
        Write-Host "Found $winfx2. Copy it as $winfx"
        Copy-Item $winfx2 $winfx
    }
}