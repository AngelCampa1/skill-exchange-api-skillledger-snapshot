#!/bin/bash

# SkillLedger Development Environment Startup Script
# This script starts all necessary services for local development

set -e

echo "🚀 Starting SkillLedger Development Environment..."
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running. Please start Docker and try again.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Docker is running${NC}"
echo ""

# Check if docker-compose exists
if ! command -v docker-compose &> /dev/null; then
    echo -e "${YELLOW}⚠️  docker-compose not found, trying 'docker compose'${NC}"
    DOCKER_COMPOSE="docker compose"
else
    DOCKER_COMPOSE="docker-compose"
fi

# Start SQL Server container
echo "📦 Starting SQL Server container..."
$DOCKER_COMPOSE up -d sqlserver

# Wait for SQL Server to be ready
echo "⏳ Waiting for SQL Server to be ready..."
MAX_ATTEMPTS=30
ATTEMPT=0

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if docker exec skillledger-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SkillLedger2024! -Q "SELECT 1" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ SQL Server is ready!${NC}"
        break
    fi

    ATTEMPT=$((ATTEMPT + 1))
    echo -n "."
    sleep 2

    if [ $ATTEMPT -eq $MAX_ATTEMPTS ]; then
        echo -e "\n${RED}❌ SQL Server failed to start after 60 seconds${NC}"
        echo "Check logs with: docker logs skillledger-sqlserver"
        exit 1
    fi
done

echo ""

# Run database migrations
echo "🔄 Running database migrations..."
cd "$(dirname "$0")/.."

if dotnet ef database update --project src/SkillLedger.Api; then
    echo -e "${GREEN}✅ Database migrations completed${NC}"
else
    echo -e "${YELLOW}⚠️  No migrations to apply or migration failed${NC}"
    echo "   This is normal for first-time setup or if no migrations exist yet"
fi

echo ""
echo -e "${GREEN}✅ Development environment is ready!${NC}"
echo ""
echo "📋 Next steps:"
echo "   1. Start the backend:  cd src/SkillLedger.Api && dotnet run"
echo "   2. Start the frontend: cd web && yarn dev"
echo ""
echo "🔗 Services:"
echo "   Backend API:    http://localhost:8030"
echo "   Frontend:       http://localhost:3030"
echo "   SQL Server:     localhost:9030"
echo ""
echo "🔑 Database Credentials:"
echo "   Server:   localhost,9030"
echo "   Database: SkillLedgerDb_Dev"
echo "   User:     sa"
echo "   Password: SkillLedger2024!"
echo ""
echo "🛑 To stop services:"
echo "   docker-compose down"
echo ""