@echo off
REM ============================================================================
REM run-app.bat — Coffee Shop POS Application Startup Script (Windows CMD)
REM ============================================================================
REM This batch script:
REM   1. Checks if PostgreSQL is installed
REM   2. Creates the coffee_shop_pos database
REM   3. Runs the schema and seed data scripts
REM   4. Builds and runs the application
REM
REM Prerequisites:
REM   - PostgreSQL must be installed
REM   - PostgreSQL must be added to PATH
REM   - PostgreSQL service must be running
REM
REM Usage: run-app.bat
REM ============================================================================

setlocal enabledelayedexpansion

REM Configuration
set POSTGRES_HOST=localhost
set POSTGRES_PORT=5432
set POSTGRES_USER=postgres
set POSTGRES_PASSWORD=123
set POSTGRES_DB=coffee_shop_pos

echo.
echo ====================================================================
echo    Coffee Shop POS - Application Startup Script (Windows CMD)
echo ====================================================================
echo.

REM Change to script directory
cd /d "%~dp0"

REM ────────────────────────────────────────────────────────────────────
REM Step 1: Check PostgreSQL Installation
REM ────────────────────────────────────────────────────────────────────
echo [1/5] Checking PostgreSQL Installation...
psql --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: PostgreSQL not found or not in PATH
    echo Please install PostgreSQL and add it to your PATH environment variable
    pause
    exit /b 1
)
for /f "tokens=*" %%i in ('psql --version 2^>nul') do set PGVERSION=%%i
echo SUCCESS: PostgreSQL found: %PGVERSION%

REM ────────────────────────────────────────────────────────────────────
REM Step 2: Test Connection
REM ────────────────────────────────────────────────────────────────────
echo.
echo [2/5] Testing PostgreSQL Connection...
set PGPASSWORD=%POSTGRES_PASSWORD%
psql -h %POSTGRES_HOST% -p %POSTGRES_PORT% -U %POSTGRES_USER% -d postgres -c "SELECT 1;" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Cannot connect to PostgreSQL
    echo  - Host: %POSTGRES_HOST%
    echo  - Port: %POSTGRES_PORT%
    echo  - User: %POSTGRES_USER%
    echo Please ensure PostgreSQL is running and credentials are correct
    pause
    exit /b 1
)
echo SUCCESS: PostgreSQL connection established

REM ────────────────────────────────────────────────────────────────────
REM Step 3: Setup Database
REM ────────────────────────────────────────────────────────────────────
echo.
echo [3/5] Setting up Database...
echo  - Creating database '%POSTGRES_DB%'...
psql -h %POSTGRES_HOST% -p %POSTGRES_PORT% -U %POSTGRES_USER% -d postgres -c "CREATE DATABASE %POSTGRES_DB%;" >nul 2>&1
if errorlevel 1 (
    echo  - Database already exists, continuing...
) else (
    echo  - SUCCESS: Database created
)

echo  - Running schema script...
if exist "Database\schema_postgresql.sql" (
    psql -h %POSTGRES_HOST% -p %POSTGRES_PORT% -U %POSTGRES_USER% -d %POSTGRES_DB% -f "Database\schema_postgresql.sql" >nul 2>&1
    if errorlevel 1 (
        echo  - Schema already exists
    ) else (
        echo  - SUCCESS: Schema created
    )
) else (
    echo  - ERROR: schema_postgresql.sql not found
    pause
    exit /b 1
)

echo  - Running seed data script...
if exist "Database\seed_data_postgresql.sql" (
    psql -h %POSTGRES_HOST% -p %POSTGRES_PORT% -U %POSTGRES_USER% -d %POSTGRES_DB% -f "Database\seed_data_postgresql.sql" >nul 2>&1
    if errorlevel 1 (
        echo  - Seed data already loaded
    ) else (
        echo  - SUCCESS: Seed data loaded
    )
) else (
    echo  - WARNING: seed_data_postgresql.sql not found
)

REM ────────────────────────────────────────────────────────────────────
REM Step 4: Build Application
REM ────────────────────────────────────────────────────────────────────
echo.
echo [4/5] Building Application...
if exist "CoffeeShopPOS\CoffeeShopPOS.csproj" (
    dotnet build CoffeeShopPOS\CoffeeShopPOS.csproj -c Release >nul 2>&1
    if errorlevel 1 (
        echo ERROR: Failed to build application
        pause
        exit /b 1
    )
    echo SUCCESS: Application built
) else (
    echo ERROR: CoffeeShopPOS.csproj not found
    pause
    exit /b 1
)

REM ────────────────────────────────────────────────────────────────────
REM Step 5: Launch Application
REM ────────────────────────────────────────────────────────────────────
echo.
echo [5/5] Launching Coffee Shop POS Application...
echo ====================================================================
echo SUCCESS: Application starting...
echo  - Database: %POSTGRES_DB%
echo  - Host: %POSTGRES_HOST%
echo  - Port: %POSTGRES_PORT%
echo.

set PGPASSWORD=
dotnet run --project CoffeeShopPOS --configuration Release

pause
