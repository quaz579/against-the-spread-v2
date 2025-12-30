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

# Wait for SQL Server to be ready (using TCP connection check)
echo "Waiting for SQL Server to be ready..."
MAX_RETRIES=60
RETRY_COUNT=0
until nc -z localhost 1433 2>/dev/null; do
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
        echo "Error: SQL Server port not available after $MAX_RETRIES attempts"
        exit 1
    fi
    echo "  Waiting for SQL Server port... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done
echo "SQL Server port is open, waiting for service to initialize..."
sleep 10

# Create database using dotnet (more reliable than sqlcmd in container)
echo "Creating database via EF Core..."

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
