#!/bin/bash
set -euo pipefail

# ToDoList Deploy Script
# Usage: ./deploy.sh api|sql <file>|status

# ── Server Config ──────────────────────────────────────────────
API_HOST="192.168.74.122"
API_USER="kevin"
API_PATH="/opt/todolist"
API_SERVICE="todolist-api"

SRC_DIR="$(cd "$(dirname "$0")/../backend" && pwd)"
PUBLISH_DIR="/tmp/todolist-publish"

# ── Colors ─────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log()  { echo -e "${CYAN}[deploy]${NC} $1"; }
ok()   { echo -e "${GREEN}[  ok  ]${NC} $1"; }
warn() { echo -e "${YELLOW}[ warn ]${NC} $1"; }
err()  { echo -e "${RED}[error ]${NC} $1"; exit 1; }

# ── Functions ──────────────────────────────────────────────────

publish_api() {
    log "1/4 Publishing ToDoList.Api..."
    rm -rf "$PUBLISH_DIR"
    dotnet publish "$SRC_DIR/src/ToDoList.Api/ToDoList.Api.csproj" \
        -c Release \
        -o "$PUBLISH_DIR" \
        --nologo \
        -v quiet
    ok "Published ToDoList.Api → $PUBLISH_DIR"
}

deploy_api() {
    publish_api

    log "2/4 Packaging..."
    local tarball="/tmp/todolist-api.tar.gz"
    tar -czf "$tarball" -C "$PUBLISH_DIR" .
    ok "Created $tarball ($(du -h "$tarball" | cut -f1))"

    log "3/4 Uploading to ${API_USER}@${API_HOST}..."
    scp -q "$tarball" "${API_USER}@${API_HOST}:/tmp/todolist-api.tar.gz"
    ok "Uploaded to $API_HOST"

    log "4/4 Deploying on $API_HOST..."
    ssh "${API_USER}@${API_HOST}" bash -s <<EOF
        set -e
        sudo systemctl stop $API_SERVICE 2>/dev/null || true

        # Preserve config
        if [ -f "$API_PATH/appsettings.Production.json" ]; then
            cp "$API_PATH/appsettings.Production.json" /tmp/appsettings.Production.json.bak
        fi

        sudo mkdir -p $API_PATH
        sudo rm -rf $API_PATH/*
        sudo tar -xzf /tmp/todolist-api.tar.gz -C $API_PATH
        sudo chown -R www-data:www-data $API_PATH 2>/dev/null || true

        # Restore config
        if [ -f /tmp/appsettings.Production.json.bak ]; then
            sudo cp /tmp/appsettings.Production.json.bak $API_PATH/appsettings.Production.json
        fi

        sudo systemctl start $API_SERVICE
        sleep 2
        if systemctl is-active --quiet $API_SERVICE; then
            echo "✓ $API_SERVICE is running"
        else
            echo "✗ $API_SERVICE failed to start"
            sudo journalctl -u $API_SERVICE --no-pager -n 20
            exit 1
        fi
EOF
    ok "API deployed and running on $API_HOST"
    rm -f "$tarball"
}

deploy_sql() {
    local sqlfile=$1
    if [ ! -f "$sqlfile" ]; then
        err "SQL file not found: $sqlfile"
    fi
    log "Running $sqlfile on $API_HOST..."
    ssh "${API_USER}@${API_HOST}" "mysql -u todoapp -p todolist" < "$sqlfile"
    ok "SQL script applied: $(basename "$sqlfile")"
}

check_status() {
    log "Checking $API_SERVICE on $API_HOST..."
    echo ""
    ssh "${API_USER}@${API_HOST}" bash -s <<EOF
        if systemctl is-active --quiet $API_SERVICE; then
            echo "  ✓ $API_SERVICE is running"
        else
            echo "  ✗ $API_SERVICE is NOT running"
        fi
        echo ""
        echo "Recent logs:"
        sudo journalctl -u $API_SERVICE --no-pager -n 10 2>/dev/null || echo "  (no logs)"
EOF
}

# ── Main ───────────────────────────────────────────────────────

if [ $# -lt 1 ]; then
    echo "Usage: $0 api|sql <file>|status"
    echo ""
    echo "  api       Publish and deploy API to $API_HOST"
    echo "  sql <f>   Run a SQL script on the server's MySQL"
    echo "  status    Check service status and recent logs"
    exit 1
fi

case "$1" in
    api)
        deploy_api
        ;;
    sql)
        [ $# -lt 2 ] && err "Usage: $0 sql <file.sql>"
        deploy_sql "$2"
        ;;
    status)
        check_status
        ;;
    *)
        err "Unknown command: $1. Use api|sql|status"
        ;;
esac

echo ""
ok "Done!"
