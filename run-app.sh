#!/bin/bash
# ============================================================================
# run-app.sh — Coffee Shop POS Application Startup Script (Linux/macOS)
# ============================================================================
# This script:
#   1. Checks if PostgreSQL is installed and running
#   2. Creates the coffee_shop_pos database
#   3. Runs the schema and seed data scripts
#   4. Builds and runs the application
#
# Prerequisites:
#   - PostgreSQL must be installed and running
#   - psql must be in PATH
#   - .NET 8 SDK must be installed
#
# Usage: ./run-app.sh
# ============================================================================

set -e

# Configuration
POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-123}"
POSTGRES_DB="coffee_shop_pos"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# ────────────────────────────────────────────────────────────────────────
# Helper Functions
# ────────────────────────────────────────────────────────────────────────

print_header() {
    echo -e "${CYAN}"
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║   Coffee Shop POS — Application Startup Script (Linux/macOS)   ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

print_step() {
    echo -e "${YELLOW}[$1/5] $2${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${CYAN}ℹ $1${NC}"
}

# ────────────────────────────────────────────────────────────────────────
# Main Script
# ────────────────────────────────────────────────────────────────────────

print_header

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# ────────────────────────────────────────────────────────────────────────
# Step 1: Check PostgreSQL Installation
# ────────────────────────────────────────────────────────────────────────
print_step 1 "Checking PostgreSQL Installation..."

if ! command -v psql &> /dev/null; then
    print_error "PostgreSQL not found"
    echo "  Please install PostgreSQL:"
    echo "  Ubuntu/Debian: sudo apt-get install postgresql-client"
    echo "  macOS: brew install postgresql"
    echo "  Or download from: https://www.postgresql.org/download/"
    exit 1
fi

PG_VERSION=$(psql --version)
print_success "PostgreSQL found: $PG_VERSION"

# ────────────────────────────────────────────────────────────────────────
# Step 2: Test PostgreSQL Connection
# ────────────────────────────────────────────────────────────────────────
print_step 2 "Testing PostgreSQL Connection..."

export PGPASSWORD="$POSTGRES_PASSWORD"

if ! psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d postgres -c "SELECT 1;" &> /dev/null; then
    print_error "Failed to connect to PostgreSQL"
    echo "  Host: $POSTGRES_HOST"
    echo "  Port: $POSTGRES_PORT"
    echo "  User: $POSTGRES_USER"
    echo ""
    echo "  Make sure PostgreSQL is running:"
    echo "  Ubuntu/Debian: sudo systemctl start postgresql"
    echo "  macOS: brew services start postgresql"
    exit 1
fi

print_success "PostgreSQL connection successful"

# ────────────────────────────────────────────────────────────────────────
# Step 3: Setup Database
# ────────────────────────────────────────────────────────────────────────
print_step 3 "Setting up Database..."

echo "  Creating database '$POSTGRES_DB'..."
if psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d postgres -c "CREATE DATABASE $POSTGRES_DB;" &> /dev/null; then
    print_success "Database created"
else
    print_info "Database already exists, continuing..."
fi

echo "  Running schema script..."
if [ ! -f "Database/schema_postgresql.sql" ]; then
    print_error "schema_postgresql.sql not found"
    exit 1
fi

if psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f "Database/schema_postgresql.sql" &> /dev/null; then
    print_success "Schema created successfully"
else
    print_info "Schema already exists or already executed"
fi

echo "  Running seed data script..."
if [ -f "Database/seed_data_postgresql.sql" ]; then
    if psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f "Database/seed_data_postgresql.sql" &> /dev/null; then
        print_success "Seed data loaded successfully"
    else
        print_info "Seed data already loaded"
    fi
else
    echo -e "${YELLOW}⚠ seed_data_postgresql.sql not found (optional)${NC}"
fi

# ────────────────────────────────────────────────────────────────────────
# Step 4: Build Application
# ────────────────────────────────────────────────────────────────────────
print_step 4 "Building Application..."

if [ ! -f "CoffeeShopPOS/CoffeeShopPOS.csproj" ]; then
    print_error "CoffeeShopPOS.csproj not found"
    exit 1
fi

if dotnet build CoffeeShopPOS/CoffeeShopPOS.csproj -c Release > /dev/null 2>&1; then
    print_success "Application built successfully"
else
    print_error "Failed to build application"
    exit 1
fi

# ────────────────────────────────────────────────────────────────────────
# Step 5: Launch Application
# ────────────────────────────────────────────────────────────────────────
print_step 5 "Launching Coffee Shop POS Application..."
echo "────────────────────────────────────────────────────────────────"

print_success "Starting application..."
echo "  Database: $POSTGRES_DB"
echo "  Host: $POSTGRES_HOST"
echo "  Port: $POSTGRES_PORT"
echo ""
echo -e "${CYAN}Application starting. Please wait...${NC}"

# Clear the PostgreSQL password from environment
unset PGPASSWORD

# Run the application
dotnet run --project CoffeeShopPOS --configuration Release
