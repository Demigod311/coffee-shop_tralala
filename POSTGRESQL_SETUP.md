# PostgreSQL Configuration Guide

This document explains how to set up and run the Coffee Shop POS application with PostgreSQL.

## Prerequisites

### 1. PostgreSQL Installation

**Windows:**
- Download from: https://www.postgresql.org/download/windows/
- Run the installer and follow the setup wizard
- Remember the PostgreSQL password you set during installation
- Add PostgreSQL `bin` folder to your PATH (usually `C:\Program Files\PostgreSQL\16\bin`)

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install postgresql postgresql-contrib
sudo systemctl start postgresql
```

**macOS:**
```bash
brew install postgresql
brew services start postgresql
```

### 2. .NET 8 SDK

Download and install from: https://dotnet.microsoft.com/download/dotnet/8.0

## Project Structure

```
coffee-shop_tralala/
├── run-app.ps1                    # PowerShell startup script (Windows)
├── run-app.bat                    # Batch startup script (Windows)
├── run-app.sh                     # Bash startup script (Linux/macOS)
├── Database/
│   ├── schema_postgresql.sql      # PostgreSQL schema (replaces MySQL)
│   ├── seed_data_postgresql.sql   # Sample data for PostgreSQL
│   ├── DbHelper.cs                # Updated to use Npgsql
│   └── ...
├── CoffeeShopPOS/
│   ├── CoffeeShopPOS.csproj       # Updated to use Npgsql
│   ├── Program.cs
│   └── ...
```

## Database Configuration

The application is configured to connect to PostgreSQL using these default credentials:

- **Host:** `localhost`
- **Port:** `5432`
- **Database:** `coffee_shop_pos`
- **Username:** `postgres`
- **Password:** `123`

To use different credentials, modify the scripts or the connection string in `DbHelper.cs`:

```csharp
// In CoffeeShopPOS/Database/DbHelper.cs
private static string _connectionString = 
  "Host=localhost;Port=5432;Database=coffee_shop_pos;Username=postgres;Password=123;";
```

## Running the Application

### Option 1: Automatic Startup Script (Recommended)

**Windows (PowerShell):**
```powershell
.\run-app.ps1
# Or with custom credentials:
.\run-app.ps1 -PostgresPassword "your_password" -PostgresUser "your_user"
```

**Windows (Command Prompt):**
```cmd
run-app.bat
# Edit the batch file to change POSTGRES_PASSWORD if needed
```

**Linux/macOS:**
```bash
chmod +x run-app.sh
./run-app.sh
# Or with custom credentials:
POSTGRES_PASSWORD="your_password" ./run-app.sh
```

### Option 2: Manual Setup

**Step 1: Create the database**
```bash
createdb -U postgres coffee_shop_pos
```

**Step 2: Run the schema**
```bash
psql -U postgres -d coffee_shop_pos -f Database/schema_postgresql.sql
```

**Step 3: Load sample data**
```bash
psql -U postgres -d coffee_shop_pos -f Database/seed_data_postgresql.sql
```

**Step 4: Build and run the application**
```bash
dotnet build CoffeeShopPOS/CoffeeShopPOS.csproj
dotnet run --project CoffeeShopPOS
```

## Testing the Setup

After running the startup script or manual setup, the application should launch. You can log in with the default test credentials:

- **Username:** `admin`
- **Password:** `admin123` (you'll need to set this manually or use BCrypt to hash it)
- **Username:** `manager1`
- **Username:** `cashier1`
- **Username:** `waiter1`

> Note: The seed data contains placeholder password hashes. For production, ensure proper authentication setup.

## Database Differences from MySQL

Key changes made for PostgreSQL compatibility:

| Feature | MySQL | PostgreSQL |
|---------|-------|------------|
| Serial IDs | `INT AUTO_INCREMENT` | `SERIAL` |
| Boolean | `TINYINT(1)` | `BOOLEAN` |
| Enums | `ENUM('a','b')` | Type-safe `ENUM` types |
| Timestamps | `DATETIME DEFAULT CURRENT_TIMESTAMP` | `TIMESTAMP DEFAULT CURRENT_TIMESTAMP` |
| Character Set | Native UTF-8 support | UTF-8 by default |
| Driver | MySql.Data | Npgsql |

## Connection String Reference

**Default:**
```
Host=localhost;Port=5432;Database=coffee_shop_pos;Username=postgres;Password=123;
```

**With SSL (Production):**
```
Host=your-host.com;Port=5432;Database=coffee_shop_pos;Username=postgres;Password=123;SSL Mode=Require;
```

**Custom Port:**
```
Host=localhost;Port=5433;Database=coffee_shop_pos;Username=postgres;Password=123;
```

## Troubleshooting

### Connection Failed
- **Check if PostgreSQL is running:**
  - Windows: Services > PostgreSQL
  - Linux: `sudo systemctl status postgresql`
  - macOS: `brew services list`
- **Verify credentials** in the startup script
- **Check if database exists:** `psql -l`

### Schema Already Exists
- The scripts safely handle re-running: `CREATE TABLE IF NOT EXISTS`
- To reset: `dropdb coffee_shop_pos` then rerun the setup

### psql Command Not Found
- **Windows:** Add PostgreSQL bin folder to PATH
- **Linux:** Install postgresql-client
- **macOS:** Reinstall PostgreSQL with Homebrew

### Database Connection in Application
- Ensure `DbHelper.cs` has the correct connection string
- The connection string is in `CoffeeShopPOS/Database/DbHelper.cs`
- Modify the `_connectionString` variable if needed

## Resetting the Database

To completely reset and start fresh:

```bash
# PostgreSQL
dropdb -U postgres coffee_shop_pos
createdb -U postgres coffee_shop_pos
psql -U postgres -d coffee_shop_pos -f Database/schema_postgresql.sql
psql -U postgres -d coffee_shop_pos -f Database/seed_data_postgresql.sql
```

## Next Steps

1. Run the application using one of the startup scripts
2. Log in with test credentials (`admin` / `admin123`)
3. Configure the application in the Admin panel
4. Set proper passwords for all users
5. Create your menu items, categories, and inventory

## Support

For PostgreSQL-specific issues:
- PostgreSQL Documentation: https://www.postgresql.org/docs/
- Npgsql Documentation: https://www.npgsql.org/doc/

For application-specific issues:
- Check `Database/DbHelper.cs` for connection details
- Review logs in the application's diagnostic panel
