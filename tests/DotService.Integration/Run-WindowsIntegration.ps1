$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$manifest = Join-Path $PSScriptRoot "dotservice.integration.json"
$cliProject = Join-Path $PSScriptRoot "..\..\src\DotService.Cli"
$exampleProject = Join-Path $PSScriptRoot "..\..\examples\DotService.Example\DotService.Example.csproj"
$serviceName = "DotService.Example"

try {
    dotnet publish $exampleProject -c Release --no-restore
    dotnet run --project $cliProject -c Release --no-build -- validate $manifest
    dotnet run --project $cliProject -c Release --no-build -- doctor $manifest
    dotnet run --project $cliProject -c Release --no-build -- install $manifest --timeout 45

    $installed = Get-Service -Name $serviceName
    if ($installed.Status -ne "Running") {
        throw "Expected $serviceName to be Running after installation; received $($installed.Status)."
    }

    dotnet run --project $cliProject -c Release --no-build -- status $serviceName
    dotnet run --project $cliProject -c Release --no-build -- stop $serviceName --timeout 45
    dotnet run --project $cliProject -c Release --no-build -- start $serviceName --timeout 45
}
finally {
    $PSNativeCommandUseErrorActionPreference = $false
    dotnet run --project $cliProject -c Release --no-build -- uninstall $serviceName --timeout 45
    $cleanupExitCode = $LASTEXITCODE
    if ($cleanupExitCode -notin 0, 3) {
        throw "Service cleanup failed with exit code $cleanupExitCode."
    }
}

if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    throw "$serviceName remained installed after integration-test cleanup."
}

Write-Output "Windows SCM integration test passed and removed its disposable service."

