#!/bin/bash
set -euo pipefail

# ToDoList Deploy Script
# Usage: ./deploy.sh api|linux|sql <file>|status

# ── Server Config ──────────────────────────────────────────────
HOST="192.168.74.122"
USER="kevin"
API_PATH="/opt/todolist"
API_SERVICE="todolist-api"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/../backend"
LINUX_SRC_DIR="$SCRIPT_DIR/../clients/linux"
LINUX_REMOTE_DIR="/home/$USER/todolist-linux"
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

    log "3/4 Uploading to ${USER}@${HOST}..."
    scp -q "$tarball" "${USER}@${HOST}:/tmp/todolist-api.tar.gz"
    ok "Uploaded to $HOST"

    log "4/4 Deploying on $HOST..."
    ssh "${USER}@${HOST}" bash -s <<EOF
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
    ok "API deployed and running on $HOST"
    rm -f "$tarball"
}

deploy_linux() {
    if [ ! -d "$LINUX_SRC_DIR/src" ]; then
        err "Linux client source not found at $LINUX_SRC_DIR/src"
    fi

    log "1/3 Syncing source to ${USER}@${HOST}:${LINUX_REMOTE_DIR}..."
    ssh "${USER}@${HOST}" "mkdir -p $LINUX_REMOTE_DIR"
    scp -q "$LINUX_SRC_DIR/meson.build" "${USER}@${HOST}:${LINUX_REMOTE_DIR}/"
    scp -qr "$LINUX_SRC_DIR/src" "${USER}@${HOST}:${LINUX_REMOTE_DIR}/"
    ok "Source uploaded"

    log "2/3 Building on $HOST..."
    ssh "${USER}@${HOST}" bash -s <<EOF
        set -e
        cd $LINUX_REMOTE_DIR

        if [ ! -d builddir ]; then
            meson setup builddir
        fi

        ninja -C builddir
EOF
    ok "Build succeeded"

    log "3/3 Installing..."
    ssh "${USER}@${HOST}" bash -s <<EOF
        set -e
        cd $LINUX_REMOTE_DIR
        sudo ninja -C builddir install
EOF
    ok "Installed to /usr/local/bin/todo-list on $HOST"
    echo ""
    log "Run with: todo-list --server http://localhost:5000"
}

deploy_sql() {
    local sqlfile=$1
    if [ ! -f "$sqlfile" ]; then
        err "SQL file not found: $sqlfile"
    fi
    log "Running $sqlfile on $HOST..."
    ssh "${USER}@${HOST}" "mysql -u todoapp -p todolist" < "$sqlfile"
    ok "SQL script applied: $(basename "$sqlfile")"
}

check_status() {
    log "Checking $API_SERVICE on $HOST..."
    echo ""
    ssh "${USER}@${HOST}" bash -s <<EOF
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
    echo "Usage: $0 api|linux|sql <file>|status"
    echo ""
    echo "  api       Publish and deploy API to $HOST"
    echo "  linux     Build and install Linux client on $HOST"
    echo "  sql <f>   Run a SQL script on the server's MySQL"
    echo "  status    Check service status and recent logs"
    exit 1
fi

case "$1" in
    api)
        deploy_api
        ;;
    linux)
        deploy_linux
        ;;
    sql)
        [ $# -lt 2 ] && err "Usage: $0 sql <file.sql>"
        deploy_sql "$2"
        ;;
    status)
        check_status
        ;;
    *)
        err "Unknown command: $1. Use api|linux|sql|status"
        ;;
esac

echo ""
ok "Done!"
