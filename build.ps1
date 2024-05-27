# Disable command echoing
$ErrorActionPreference = "Stop"

# Set the working directory to the script's location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location -Path $scriptDir

# Define the target runtimes
$runtimes = @(
    "win-x86",
    "win-x64",
    "win-arm",
    "win-arm64"
    # Uncomment these lines to include Linux and macOS builds
    # "linux-x64",
    # "linux-musl-x64",
    # "linux-arm",
    # "linux-arm64",
    # "osx-x64",
    # "osx-arm64"
)

# Publish for each runtime
foreach ($runtime in $runtimes) {
    Write-Host "Publishing for runtime: $runtime"
    dotnet publish -c Release -r $runtime --self-contained true
}

# Change to the output directory
$outputDir = Join-Path $scriptDir "SD/bin/Release/net6.0"
Set-Location -Path $outputDir

# Archive each published runtime
foreach ($runtime in $runtimes) {
    $zipFile = "$runtime.zip"
    $runtimeDir = $runtime
    if (Test-Path $runtimeDir) {
        Write-Host "Creating archive: $zipFile"
        7z a $zipFile $runtimeDir
    } else {
        Write-Host "Skipping archive for $runtimeDir, directory not found."
    }
}

Write-Host "Done."
exit
