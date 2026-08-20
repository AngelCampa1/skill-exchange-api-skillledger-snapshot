# Docker Development Setup for SkillLedger

This guide explains how to set up the SkillLedger development environment with Docker SQL Server on Linux/WSL2.

## Problem Solved

SQL Server LocalDB is not supported on Linux/WSL2. This Docker setup provides a full SQL Server instance that works across all platforms.

## Prerequisites

- Docker Desktop (for Windows/Mac) or Docker Engine (for Linux)
- Docker Compose
- .NET 9 SDK
- Node.js 18+ and Yarn

## Quick Start

### 1. Start the Development Environment

```bash
./scripts/start-dev.sh
```

This script will:
- ✅ Check if Docker is running
- ✅ Start SQL Server container
- ✅ Wait for SQL Server to be ready
- ✅ Run database migrations
- ✅ Display connection information

### 2. Start the Backend API

```bash
cd src/SkillLedger.Api
dotnet run
```

Backend will be available at: http://localhost:5000

### 3. Start the Frontend

```bash
cd web
yarn dev
```

Frontend will be available at: http://localhost:3000

### 4. Stop Services

```bash
./scripts/stop-dev.sh
```

## Manual Docker Commands

### Start SQL Server

```bash
docker-compose up -d sqlserver
```

### Check SQL Server Status

```bash
docker ps
docker logs skillledger-sqlserver
```

### Connect to SQL Server

```bash
docker exec -it skillledger-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SkillLedger2024!
```

### Stop SQL Server

```bash
docker-compose down
```

### Remove All Data (⚠️ Destructive)

```bash
docker-compose down -v
```

## Database Connection Details

| Property | Value |
|----------|-------|
| Server | localhost,1433 |
| Database | SkillLedgerDb_Dev |
| Username | sa |
| Password | SkillLedger2024! |
| Trust Certificate | Yes |

## Connection String

```
Server=localhost,1433;Database=SkillLedgerDb_Dev;User Id=sa;Password=SkillLedger2024!;TrustServerCertificate=true;MultipleActiveResultSets=true
```

## Database Migrations

### Create a New Migration

```bash
cd src/SkillLedger.Api
dotnet ef migrations add MigrationName
```

### Apply Migrations

```bash
cd src/SkillLedger.Api
dotnet ef database update
```

### Rollback Migration

```bash
cd src/SkillLedger.Api
dotnet ef database update PreviousMigrationName
```

### Remove Last Migration

```bash
cd src/SkillLedger.Api
dotnet ef migrations remove
```

## Troubleshooting

### Docker is not running
**Error**: `Cannot connect to the Docker daemon`

**Solution**: Start Docker Desktop or Docker Engine

```bash
# Check Docker status
docker info

# On Linux, start Docker
sudo systemctl start docker
```

### SQL Server won't start
**Error**: Container exits immediately

**Solution**: Check logs and available memory

```bash
# View logs
docker logs skillledger-sqlserver

# SQL Server requires at least 2GB RAM
# Increase Docker Desktop memory allocation if needed
```

### Port 1433 already in use
**Error**: `port is already allocated`

**Solution**: Stop the process using port 1433

```bash
# Find process using port 1433
sudo lsof -ti:1433

# Kill the process
sudo kill -9 $(sudo lsof -ti:1433)

# Or use a different port in docker-compose.yml
```

### Cannot connect to database
**Error**: `A network-related or instance-specific error occurred`

**Solution**:
1. Ensure SQL Server container is running: `docker ps`
2. Wait for SQL Server to be ready (can take 10-30 seconds)
3. Check firewall isn't blocking port 1433
4. Verify connection string in appsettings.Development.json

### Migrations fail
**Error**: `Unable to create an object of type 'SkillLedgerDbContext'`

**Solution**: Ensure you're in the correct directory

```bash
cd src/SkillLedger.Api
dotnet ef database update
```

## Data Persistence

Data is persisted in a Docker volume named `skillledger-sqldata`. This means:

✅ Data survives container restarts
✅ Data survives `docker-compose down`
❌ Data is lost with `docker-compose down -v`

## Security Notes

⚠️ **Development Only**: The password `SkillLedger2024!` is for local development only.

For production:
- Use Azure SQL Database or managed SQL Server
- Store credentials in Azure Key Vault
- Use Managed Identity for authentication
- Never commit passwords to source control

## Files Modified

1. `docker-compose.yml` - Docker SQL Server configuration
2. `src/SkillLedger.Api/appsettings.Development.json` - Connection string updated
3. `scripts/start-dev.sh` - Development startup script
4. `scripts/stop-dev.sh` - Development shutdown script

## Next Steps

After setup is complete:
1. Test user registration at http://localhost:3000/register
2. Verify database tables were created:
   ```sql
   SELECT name FROM sys.tables;
   ```
3. Check audit logs are being written
4. Test email verification flow

## Resources

- [SQL Server on Docker](https://hub.docker.com/_/microsoft-mssql-server)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)