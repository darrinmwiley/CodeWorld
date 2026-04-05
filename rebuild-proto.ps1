# rebuild-proto.ps1
# Regenerates C# gRPC stubs from game.proto, then rebuilds the Java server.
# Run from the project root: .\rebuild-proto.ps1

param(
    [switch]$JavaOnly,  # Only rebuild java-server
    [switch]$ProtoOnly  # Only regenerate C# stubs
)

$ProjectRoot = $PSScriptRoot
$ProtoFile   = Join-Path $ProjectRoot "proto\game.proto"
$OutDir      = Join-Path $ProjectRoot "Assets\Rpc\Generated"
$JavaServer  = Join-Path $ProjectRoot "java-server"
$GrpcPlugin  = "C:\Users\Darrin\.nuget\packages\grpc.tools\2.66.0\tools\windows_x64\grpc_csharp_plugin.exe"

$ExitCode = 0

# === Step 1: Regenerate C# stubs ===
if (-not $JavaOnly) {
    Write-Host "==> Regenerating C# gRPC stubs..." -ForegroundColor Cyan

    if (-not (Test-Path $GrpcPlugin)) {
        Write-Host "ERROR: grpc_csharp_plugin not found at:" -ForegroundColor Red
        Write-Host "  $GrpcPlugin" -ForegroundColor Red
        Write-Host "Install grpc.tools or update the path in rebuild-proto.ps1" -ForegroundColor Yellow
        $ExitCode = 1
    } else {
        $protocArgs = @(
            "-I=proto",
            "--csharp_out=$OutDir",
            "--grpc_out=$OutDir",
            "--plugin=protoc-gen-grpc=$GrpcPlugin",
            "proto\game.proto"
        )

        & protoc @protocArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: protoc failed (exit $LASTEXITCODE)" -ForegroundColor Red
            $ExitCode = $LASTEXITCODE
        } else {
            Write-Host "  C# stubs generated in: $OutDir" -ForegroundColor Green
        }
    }
}

# === Step 2: Rebuild Java server ===
if (-not $ProtoOnly -and $ExitCode -eq 0) {
    Write-Host "==> Building Java server..." -ForegroundColor Cyan

    Push-Location $JavaServer
    try {
        & .\gradlew.bat build --quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Gradle build failed (exit $LASTEXITCODE)" -ForegroundColor Red
            $ExitCode = $LASTEXITCODE
        } else {
            Write-Host "  Java server built successfully." -ForegroundColor Green
        }
    } finally {
        Pop-Location
    }
}

if ($ExitCode -eq 0) {
    Write-Host ""
    Write-Host "All done! You can now enter Play mode." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Build failed. Check errors above." -ForegroundColor Red
}

exit $ExitCode
