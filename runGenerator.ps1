param (
    [string]$SolutionPath,
    [string]$GeneratorProjectPath,
    [string]$ConsoleAppProjectPath,
    [string]$OutputPath
)

# Ensure output directory exists
if (-Not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath
}

# Build the source generator project
Write-Host "Building source generator project..."
dotnet build $GeneratorProjectPath

# Check if build was successful
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build the source generator project." -ForegroundColor Red
    exit 1
}

# Build the console application that runs the generator
Write-Host "Building console application..."
dotnet build $ConsoleAppProjectPath

# Check if build was successful
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build the console application." -ForegroundColor Red
    exit 1
}

# Run the console application to invoke the source generator
Write-Host "Running the console application to generate code..."
& "$($ConsoleAppProjectPath)\bin\Debug\net6.0\MyConsoleApp.exe"

# Check if code generation was successful
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to run the console application." -ForegroundColor Red
    exit 1
}

# Move generated files to Unity project
Write-Host "Moving generated files to Unity project..."
Move-Item -Path "path_to_generated_file\*.cs" -Destination $OutputPath -Force

Write-Host "Code generation completed successfully!" -ForegroundColor Green
