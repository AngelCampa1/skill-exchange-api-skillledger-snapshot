#!/bin/bash

# SkillLedger Development Environment Shutdown Script

set -e

echo "🛑 Stopping SkillLedger Development Environment..."
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Navigate to project root
cd "$(dirname "$0")/.."

# Check if docker-compose exists
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
else
    DOCKER_COMPOSE="docker-compose"
fi

# Stop containers
echo "📦 Stopping Docker containers..."
$DOCKER_COMPOSE down

echo -e "${GREEN}✅ All services stopped${NC}"
echo ""
echo "💾 Data is preserved in Docker volumes"
echo "   To remove data volumes: docker-compose down -v"
echo ""