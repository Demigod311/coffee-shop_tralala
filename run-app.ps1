# ============================================================================
# run-app.ps1 - Coffee Shop POS Application Startup Script (Windows)
# ============================================================================
# This script:
#   1. Checks if PostgreSQL is installed and running
#   2. Creates the coffee_shop_pos database
#   3. Runs the schema and seed data scripts
#   4. Builds and runs the application
#
# Prerequisites:
#   - PostgreSQL must be installed (https://www.postgresql.org/download/windows/)
#   - PostgreSQL should be running
#   - Set your PostgreSQL password in the parameters below if needed
#
# Usage: .\run-app.ps1
# ============================================================================

param(
    [string]$PostgresPassword = "123",
    [string]$PostgresUser = "postgres",
    [string]$PostgresHost = "localhost",
    [int]$PostgresPort = 5432,
    [string]$PsqlPath = ""
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Coffee Shop POS - Application Startup Script (Windows)" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$scriptPath = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
Set-Location $scriptPath

$psqlExe = $null
$skipDatabaseSetup = $false

if ($PsqlPath -and (Test-Path $PsqlPath)) {
    $psqlExe = (Resolve-Path $PsqlPath).Path
}
elseif (Get-Command psql -ErrorAction SilentlyContinue) {
    $psqlExe = "psql"
}
else {
    $commonPsqlPaths = @(
        "C:\Program Files\PostgreSQL\18\bin\psql.exe",
        "C:\Program Files\PostgreSQL\17\bin\psql.exe",
        "C:\Program Files\PostgreSQL\16\bin\psql.exe",
        "C:\Program Files\PostgreSQL\15\bin\psql.exe",
        "C:\Program Files\PostgreSQL\14\bin\psql.exe",
        "C:\Program Files\PostgreSQL\13\bin\psql.exe"
    )

    foreach ($candidate in $commonPsqlPaths) {
        if (Test-Path $candidate) {
            $psqlExe = $candidate
            break
        }
    }
}

# ------------------------------------------------------------------------
# Step 1: Check PostgreSQL Installation
# ------------------------------------------------------------------------
Write-Host "`n[1/5] Checking PostgreSQL Installation..." -ForegroundColor Yellow

if ($psqlExe) {
    try {
        $result = & $psqlExe --version
        Write-Host "SUCCESS: PostgreSQL found: $result" -ForegroundColor Green
    }
    catch {
        Write-Host "WARNING: Found psql but failed to execute it. Database setup will be skipped." -ForegroundColor Yellow
        $skipDatabaseSetup = $true
    }
}
else {
    Write-Host "WARNING: psql not found in PATH. Skipping database setup and continuing." -ForegroundColor Yellow
    Write-Host '  If needed, rerun with -PsqlPath "C:\Program Files\PostgreSQL\18\bin\psql.exe"' -ForegroundColor Yellow
    $skipDatabaseSetup = $true
}

# ------------------------------------------------------------------------
# Step 2: Test PostgreSQL Connection
# ------------------------------------------------------------------------
Write-Host "`n[2/5] Testing PostgreSQL Connection..." -ForegroundColor Yellow

if ($skipDatabaseSetup) {
    Write-Host "INFO: Skipped connection test because psql is unavailable." -ForegroundColor Cyan
}
else {
    $env:PGPASSWORD = $PostgresPassword
    try {
        $null = & $psqlExe -h $PostgresHost -U $PostgresUser -p $PostgresPort -d postgres -c "SELECT 1;" 2>$null
        Write-Host "SUCCESS: PostgreSQL connection successful" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: Failed to connect to PostgreSQL" -ForegroundColor Red
        Write-Host "  Host: $PostgresHost" -ForegroundColor Yellow
        Write-Host "  Port: $PostgresPort" -ForegroundColor Yellow
        Write-Host "  User: $PostgresUser" -ForegroundColor Yellow
        exit 1
    }
}

# ------------------------------------------------------------------------
# Step 3: Create Database and Run Schema
# ------------------------------------------------------------------------
Write-Host "`n[3/5] Setting up Database..." -ForegroundColor Yellow

if ($skipDatabaseSetup) {
    Write-Host "INFO: Skipped database creation/schema/seed because psql is unavailable." -ForegroundColor Cyan
}
else {
    try {
        Write-Host "  Creating database 'coffee_shop_pos'..." -ForegroundColor Gray
        & $psqlExe -h $PostgresHost -U $PostgresUser -p $PostgresPort -d postgres -c "CREATE DATABASE coffee_shop_pos;" 2>$null
        Write-Host "  SUCCESS: Database created (or already exists)" -ForegroundColor Green
    }
    catch {
        Write-Host "  INFO: Database already exists, continuing..." -ForegroundColor Cyan
    }

    try {
        Write-Host "  Running schema script..." -ForegroundColor Gray
        if (Test-Path "Database\schema_postgresql.sql") {
            & $psqlExe -h $PostgresHost -U $PostgresUser -p $PostgresPort -d coffee_shop_pos -f "Database\schema_postgresql.sql" >$null
            Write-Host "  SUCCESS: Schema created successfully" -ForegroundColor Green
        }
        else {
            Write-Host "  ERROR: schema_postgresql.sql not found" -ForegroundColor Red
            exit 1
        }
    }
    catch {
        Write-Host "  INFO: Schema already exists or already executed" -ForegroundColor Cyan
    }

    try {
        Write-Host "  Running seed data script..." -ForegroundColor Gray
        if (Test-Path "Database\seed_data_postgresql.sql") {
            & $psqlExe -h $PostgresHost -U $PostgresUser -p $PostgresPort -d coffee_shop_pos -f "Database\seed_data_postgresql.sql" >$null
            Write-Host "  SUCCESS: Seed data loaded successfully" -ForegroundColor Green
        }
        else {
            Write-Host "  WARNING: seed_data_postgresql.sql not found (optional)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  INFO: Seed data already loaded" -ForegroundColor Cyan
    }
}

# ------------------------------------------------------------------------
# Step 4: Build Application
# ------------------------------------------------------------------------
Write-Host "`n[4/5] Building Application..." -ForegroundColor Yellow

try {
    if (Test-Path "CoffeeShopPOS\CoffeeShopPOS.csproj") {
        dotnet build CoffeeShopPOS\CoffeeShopPOS.csproj -c Release >$null 2>&1
        Write-Host "SUCCESS: Application built successfully" -ForegroundColor Green
    }
    else {
        Write-Host "ERROR: CoffeeShopPOS.csproj not found" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "ERROR: Failed to build application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------------------
# Step 5: Launch Application
# ------------------------------------------------------------------------
Write-Host "`n[5/5] Launching Coffee Shop POS Application..." -ForegroundColor Yellow
Write-Host "----------------------------------------------------------------" -ForegroundColor Cyan

try {
    Write-Host "SUCCESS: Starting application..." -ForegroundColor Green
    Write-Host "  Database: coffee_shop_pos" -ForegroundColor Gray
    Write-Host "  Host: $PostgresHost" -ForegroundColor Gray
    Write-Host "  Port: $PostgresPort" -ForegroundColor Gray
    Write-Host "`nApplication starting. Please wait..." -ForegroundColor Cyan

    $env:PGPASSWORD = ""
    dotnet run --project CoffeeShopPOS --configuration Release
}
catch {
    Write-Host "`nERROR: Failed to start application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
