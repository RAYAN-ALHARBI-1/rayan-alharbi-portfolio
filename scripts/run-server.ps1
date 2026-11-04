$localDotnet = Join-Path $HOME ".dotnet10\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { "dotnet" }
$originalEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Development"

try {
    & $dotnet run --project "src\Portfolio.Server\Portfolio.Server.csproj" --urls "http://127.0.0.1:5088"
}
finally {
    if ($null -eq $originalEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $originalEnvironment
    }
}
