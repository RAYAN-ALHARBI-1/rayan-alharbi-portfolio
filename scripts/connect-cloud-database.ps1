[CmdletBinding()]
param(
    [switch]$SkipSeed
)

$ErrorActionPreference = "Stop"

function Get-PlainText {
    param([Security.SecureString]$Value)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Convert-PostgresUrlToConnectionString {
    param([string]$ConnectionValue)

    if ($ConnectionValue -notmatch "^postgres(ql)?://") {
        return $ConnectionValue
    }

    $uri = [Uri]$ConnectionValue
    if ([string]::IsNullOrWhiteSpace($uri.Host) -or [string]::IsNullOrWhiteSpace($uri.AbsolutePath.Trim('/'))) {
        throw "رابط PostgreSQL غير مكتمل. انسخ الرابط الكامل من مزود القاعدة."
    }

    $userInfo = $uri.UserInfo
    $separatorIndex = $userInfo.IndexOf(":")
    if ($separatorIndex -lt 1) {
        throw "رابط PostgreSQL يجب أن يتضمن اسم المستخدم وكلمة المرور."
    }

    $connectionBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
    $connectionBuilder["Host"] = $uri.Host
    if (-not $uri.IsDefaultPort) {
        $connectionBuilder["Port"] = $uri.Port
    }

    $connectionBuilder["Database"] = [Uri]::UnescapeDataString($uri.AbsolutePath.Trim('/'))
    $connectionBuilder["Username"] = [Uri]::UnescapeDataString($userInfo.Substring(0, $separatorIndex))
    $connectionBuilder["Password"] = [Uri]::UnescapeDataString($userInfo.Substring($separatorIndex + 1))

    $sslMode = "Require"
    foreach ($queryPart in $uri.Query.TrimStart("?").Split("&", [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $queryPair = $queryPart.Split("=", 2)
        if ($queryPair.Length -eq 2 -and $queryPair[0] -ieq "sslmode") {
            $sslMode = [Uri]::UnescapeDataString($queryPair[1])
        }
    }

    $connectionBuilder["SSL Mode"] = $sslMode
    return $connectionBuilder.ConnectionString
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $projectRoot "src\Portfolio.Server\Portfolio.Server.csproj"
$localDotnet = Join-Path $HOME ".dotnet10\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { "dotnet" }
$originalEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Development"

try {
    Write-Host "الصق رابط PostgreSQL السحابي الذي أرسله مزود القاعدة. لن يظهر أثناء الكتابة ولن يُحفظ في Git."
    $secureConnectionValue = Read-Host "رابط الاتصال" -AsSecureString
    $connectionValue = Get-PlainText -Value $secureConnectionValue

    if ([string]::IsNullOrWhiteSpace($connectionValue)) {
        throw "لم يتم إدخال رابط اتصال."
    }

    $connectionString = Convert-PostgresUrlToConnectionString -ConnectionValue $connectionValue.Trim()
    & $dotnet user-secrets --project $serverProject set "ConnectionStrings:PortfolioDb" $connectionString
    if ($LASTEXITCODE -ne 0) {
        throw "تعذر حفظ رابط الاتصال في User Secrets."
    }

    Push-Location $projectRoot
    try {
        & $dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "تعذر تجهيز أداة الترحيلات."
        }

        & $dotnet tool run dotnet-ef database update --project $serverProject --startup-project $serverProject
        if ($LASTEXITCODE -ne 0) {
            throw "تعذر إنشاء جداول قاعدة البيانات."
        }

        if (-not $SkipSeed) {
            & $dotnet run --project $serverProject -- --seed-initial-portfolio
            if ($LASTEXITCODE -ne 0) {
                throw "تم إنشاء الجداول، لكن تعذرت التعبئة الأولية للمحتوى."
            }
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "تم إعداد قاعدة البيانات السحابية بنجاح."
}
finally {
    if ($null -eq $originalEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $originalEnvironment
    }
}
