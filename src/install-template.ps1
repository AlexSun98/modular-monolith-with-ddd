# Modular Monolith Template - Installation & Usage
#
# This script helps you install and test the template locally.
# Run from the repository root directory.

param(
    [Parameter()]
    [ValidateSet("Install", "Uninstall", "Test", "Pack")]
    [string]$Action = "Install",
    
    [Parameter()]
    [string]$TestOutputPath = "../test-template-output"
)

$ErrorActionPreference = "Stop"
$templatePath = Join-Path $PSScriptRoot "src"

function Install-Template {
    Write-Host "Installing template from: $templatePath" -ForegroundColor Cyan
    
    # Uninstall first if exists
    $installed = dotnet new list modular-ddd 2>&1
    if ($installed -notmatch "No templates found") {
        Write-Host "Uninstalling existing template..." -ForegroundColor Yellow
        dotnet new uninstall $templatePath
    }
    
    # Install the template
    dotnet new install $templatePath
    
    Write-Host "`nTemplate installed! List available templates:" -ForegroundColor Green
    dotnet new list modular
}

function Uninstall-Template {
    Write-Host "Uninstalling template..." -ForegroundColor Cyan
    dotnet new uninstall $templatePath
    Write-Host "Template uninstalled." -ForegroundColor Green
}

function Test-Template {
    Write-Host "Testing template creation..." -ForegroundColor Cyan
    
    $testDir = Join-Path $PSScriptRoot $TestOutputPath
    
    # Clean up previous test
    if (Test-Path $testDir) {
        Write-Host "Removing previous test output: $testDir" -ForegroundColor Yellow
        Remove-Item -Recurse -Force $testDir
    }
    
    # Create test directory
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    Push-Location $testDir
    
    try {
        Write-Host "`nCreating solution from template..." -ForegroundColor Cyan
        dotnet new modular-ddd `
            --company-name "TestCompany" `
            --app-name "TestApp" `
            --author "Test Author" `
            --include-sample true `
            --include-tests true
        
        Write-Host "`nBuilding the solution..." -ForegroundColor Cyan
        dotnet build
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✅ Template test PASSED!" -ForegroundColor Green
            Write-Host "Test output location: $testDir" -ForegroundColor Cyan
        } else {
            Write-Host "`n❌ Template test FAILED - Build errors" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

function Pack-Template {
    Write-Host "Packing template as NuGet package..." -ForegroundColor Cyan
    
    $nuspecPath = Join-Path $PSScriptRoot "template.nuspec"
    
    if (-not (Test-Path $nuspecPath)) {
        Write-Host "Error: template.nuspec not found at $nuspecPath" -ForegroundColor Red
        exit 1
    }
    
    # Create packages directory
    $packagesDir = Join-Path $PSScriptRoot "packages"
    if (-not (Test-Path $packagesDir)) {
        New-Item -ItemType Directory -Path $packagesDir | Out-Null
    }
    
    # Pack using nuget CLI
    nuget pack $nuspecPath -OutputDirectory $packagesDir
    
    Write-Host "`nPackage created in: $packagesDir" -ForegroundColor Green
    Get-ChildItem $packagesDir -Filter "*.nupkg" | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Cyan
    }
}

# Execute the requested action
switch ($Action) {
    "Install"   { Install-Template }
    "Uninstall" { Uninstall-Template }
    "Test"      { Install-Template; Test-Template }
    "Pack"      { Pack-Template }
}

Write-Host "`n--- Usage Examples ---" -ForegroundColor Magenta
Write-Host @"

# Install template locally:
./install-template.ps1 -Action Install

# Create a new solution:
dotnet new modular-ddd -n MyCompany.MyApp --company-name MyCompany --app-name MyApp

# Create minimal solution (no sample modules):
dotnet new modular-ddd -n MyCompany.MyApp -cn MyCompany -an MyApp --include-sample false

# Test the template:
./install-template.ps1 -Action Test

# Uninstall:
./install-template.ps1 -Action Uninstall

# Pack as NuGet for distribution:
./install-template.ps1 -Action Pack

"@
