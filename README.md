# AI-Native Quality Engineer Home Task

Small ASP.NET Core Razor Pages web application with:
- Lightweight login (hard-coded test credentials)
- NZ address validation via NZ Post API
- Risk-based NUnit tests
- Playwright smoke tests
- GitHub Actions CI

## Tech Stack
- .NET 9 (ASP.NET Core Razor Pages)
- NUnit
- Playwright for .NET
- Azure App Service (deployment target)
- GitHub + GitHub Actions

## Project Structure
- `src/WebApp` - web application
- `tests/WebApp.Tests` - NUnit integration/behavior tests
- `tests/WebApp.E2E` - Playwright smoke tests
- `.github/workflows/ci.yml` - CI pipeline

## Prerequisites
- .NET SDK 9
- PowerShell (for Playwright install script)

## Configuration
Default test credentials in `src/WebApp/appsettings.json`:
- Username: `candidate`
- Password: `Passw0rd!`

For real NZ Post integration, set:
- `NzPost__ClientId` environment variable
- `NzPost__ClientSecret` environment variable

Optional overrides:
- `Login__Username`
- `Login__Password`
- `NzPost__BaseUrl`
- `NzPost__OAuthUrl`
- `NzPost__TimeoutSeconds`

## Run Locally
```powershell
dotnet restore AioiAssessment02.sln
dotnet build AioiAssessment02.sln
dotnet run --project src/WebApp/WebApp.csproj
```

Open the app URL printed in console (or use `http://127.0.0.1:5099` if started with explicit `--urls`).

## Run Tests
NUnit:
```powershell
dotnet test tests/WebApp.Tests/WebApp.Tests.csproj
```

Playwright setup + E2E:
```powershell
dotnet build tests/WebApp.E2E/WebApp.E2E.csproj
pwsh tests/WebApp.E2E/bin/Debug/net9.0/playwright.ps1 install --with-deps chromium
dotnet run --project src/WebApp/WebApp.csproj --urls http://127.0.0.1:5099
```

In another terminal:
```powershell
$env:E2E_BASE_URL="http://127.0.0.1:5099"
dotnet test tests/WebApp.E2E/WebApp.E2E.csproj
```

## CI
CI is configured in `.github/workflows/ci.yml`:
- Restore and build solution
- Run NUnit tests
- Install Playwright browser
- Start app
- Run Playwright smoke tests

## Azure Deployment (Recommended)
Deploy `src/WebApp` to Azure App Service and configure these App Settings:
- `Login__Username`
- `Login__Password`
- `NzPost__ClientId`
- `NzPost__ClientSecret`
- `NzPost__BaseUrl` (optional override)
- `NzPost__OAuthUrl` (optional override)
- `NzPost__TimeoutSeconds` (optional override)

Use GitHub Actions deployment or Azure publish profile/OIDC.


Live URL:
https://aioi-nzaddress-checker.azurewebsites.net/
Login credentials:
Username: candidate
Password: Passw0rd!

To Run Unit test:
1. Open Powershell
2. dotnet test "C:\Dev\Meena\AioiAssessment02\tests\WebApp.Tests\WebApp.Tests.csproj"

To Run E2E test:
1. To run web app, Open Powershell
    dotnet run --project "C:\Dev\Meena\AioiAssessment02\src\WebApp\WebApp.csproj" --urls http://127.0.0.1:5099
2. To run E2E test, Open another Powershell
    $env:HEADED="1"
    $env:E2E_BASE_URL="http://127.0.0.1:5099"
    dotnet test "C:\Dev\Meena\AioiAssessment02\tests/WebApp.E2E/WebApp.E2E.csproj"
    dotnet test "C:\Dev\Meena\AioiAssessment02\tests/WebApp.E2E/WebApp.E2E.csproj" --filter "Login_Succeeds_And_ShowsAddressChecker"
