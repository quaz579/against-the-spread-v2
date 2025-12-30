#!/bin/bash

# Start local development environment with SQL Server and Azurite
# Usage: ./start-local-db.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== Starting Local Development Environment ==="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker Desktop."
    exit 1
fi

# Start containers
echo "Starting SQL Server and Azurite containers..."
docker compose up -d

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
MAX_RETRIES=30
RETRY_COUNT=0
until docker exec ats-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LocalDev123!" -Q "SELECT 1" > /dev/null 2>&1; do
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
        echo "Error: SQL Server failed to start after $MAX_RETRIES attempts"
        exit 1
    fi
    echo "  Waiting for SQL Server... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done
echo "SQL Server is ready!"

# Create database if it doesn't exist
echo "Creating database if needed..."
docker exec ats-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "LocalDev123!" -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AgainstTheSpread') CREATE DATABASE AgainstTheSpread"

# Apply migrations
echo "Applying EF Core migrations..."
CONNECTION_STRING="Server=localhost,1433;Database=AgainstTheSpread;User Id=sa;Password=LocalDev123!;TrustServerCertificate=True;"
dotnet ef database update \
    --project src/AgainstTheSpread.Data \
    --startup-project src/AgainstTheSpread.Functions \
    --connection "$CONNECTION_STRING"

echo ""
echo "=== Local Development Environment Ready ==="
echo ""
echo "Services running:"
echo "  - SQL Server: localhost:1433 (sa / LocalDev123!)"
echo "  - Azurite Blob: localhost:10000"
echo "  - Azurite Queue: localhost:10001"
echo "  - Azurite Table: localhost:10002"
echo ""
echo "Connection string (already configured in local.settings.json):"
echo "  Server=localhost,1433;Database=AgainstTheSpread;User Id=sa;Password=LocalDev123!;TrustServerCertificate=True;"
echo ""
echo "Next steps:"
echo "  1. Run ./start-local.sh to start the Functions and Web app"
echo "  2. Or run ./start-e2e.sh for E2E testing with SWA CLI"
echo ""
echo "To stop: docker compose down"
echo "To stop and remove data: docker compose down -v"
