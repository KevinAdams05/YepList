#!/bin/bash
set -euo pipefail

# YepList Deploy Script
# Usage: ./deploy.sh api|linux|android|sql <file>|status

# ── Server Config ──────────────────────────────────────────────
HOST="192.168.74.122"
USER="kevin"
API_PATH="/opt/yeplist"
API_SERVICE="yeplist-api"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/../backend"
LINUX_SRC_DIR="$SCRIPT_DIR/../clients/linux"
LINUX_REMOTE_DIR="/home/$USER/yeplist-linux"
ANDROID_SRC_DIR="$SCRIPT_DIR/../clients/android"
PUBLISH_DIR="/tmp/yeplist-publish"
SHARE_DIR="//storage01/Files/YepList"

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
    log "1/4 Publishing YepList.Api..."
    rm -rf "$PUBLISH_DIR"
    dotnet publish "$SRC_DIR/src/ToDoList.Api/ToDoList.Api.csproj" \
        -c Release \
        -o "$PUBLISH_DIR" \
        --nologo \
        -v quiet
    ok "Published YepList.Api → $PUBLISH_DIR"
}

deploy_api() {
    publish_api

    log "2/4 Packaging..."
    local tarball="/tmp/yeplist-api.tar.gz"
    tar -czf "$tarball" -C "$PUBLISH_DIR" .
    ok "Created $tarball ($(du -h "$tarball" | cut -f1))"

    log "3/4 Uploading to ${USER}@${HOST}..."
    scp -q "$tarball" "${USER}@${HOST}:/tmp/yeplist-api.tar.gz"
    ok "Uploaded to $HOST"

    log "4/4 Deploying on $HOST..."
    ssh "${USER}@${HOST}" bash -s <<EOF
        set -e
        sudo systemctl stop $API_SERVICE 2>/dev/null || true

        # Preserve config (root:www-data 640 → needs sudo to read)
        if sudo test -f "$API_PATH/appsettings.Production.json"; then
            sudo cp "$API_PATH/appsettings.Production.json" /tmp/appsettings.Production.json.bak
            sudo chmod 644 /tmp/appsettings.Production.json.bak
        fi

        sudo mkdir -p $API_PATH
        sudo rm -rf $API_PATH/*
        sudo tar -xzf /tmp/yeplist-api.tar.gz -C $API_PATH
        sudo chown -R www-data:www-data $API_PATH 2>/dev/null || true

        # Restore config and re-tighten perms (config holds the DB password)
        if [ -f /tmp/appsettings.Production.json.bak ]; then
            sudo cp /tmp/appsettings.Production.json.bak $API_PATH/appsettings.Production.json
            sudo chown root:www-data $API_PATH/appsettings.Production.json
            sudo chmod 640 $API_PATH/appsettings.Production.json
            sudo rm -f /tmp/appsettings.Production.json.bak
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

    # Read version from meson.build
    local version
    version=$(grep "version:" "$LINUX_SRC_DIR/meson.build" | head -1 | sed "s/.*'\(.*\)'.*/\1/")

    log "1/6 Syncing source to ${USER}@${HOST}:${LINUX_REMOTE_DIR}..."
    ssh "${USER}@${HOST}" "mkdir -p $LINUX_REMOTE_DIR"
    scp -q "$LINUX_SRC_DIR/meson.build" "${USER}@${HOST}:${LINUX_REMOTE_DIR}/"
    scp -qr "$LINUX_SRC_DIR/src" "${USER}@${HOST}:${LINUX_REMOTE_DIR}/"
    if [ -d "$LINUX_SRC_DIR/data" ]; then
        scp -qr "$LINUX_SRC_DIR/data" "${USER}@${HOST}:${LINUX_REMOTE_DIR}/"
    fi
    ok "Source uploaded"

    log "2/6 Building on $HOST..."
    ssh "${USER}@${HOST}" bash -s <<EOF
        set -e
        cd $LINUX_REMOTE_DIR

        if [ ! -d builddir ]; then
            meson setup builddir --prefix=/usr
        else
            meson setup builddir --prefix=/usr --reconfigure
        fi

        ninja -C builddir
EOF
    ok "Build succeeded"

    log "3/6 Installing on $HOST..."
    ssh "${USER}@${HOST}" bash -s <<EOF
        set -e
        cd $LINUX_REMOTE_DIR
        sudo ninja -C builddir install
EOF
    ok "Installed to /usr/bin/yep-list on $HOST"

    log "4/6 Building .deb package on $HOST..."
    ssh "${USER}@${HOST}" bash -s <<DEBEOF
        set -e
        cd $LINUX_REMOTE_DIR

        DEB_DIR=/tmp/yeplist-deb
        rm -rf \$DEB_DIR

        # Assemble deb directory structure from build artifacts
        mkdir -p \$DEB_DIR/usr/bin
        mkdir -p \$DEB_DIR/usr/share/applications
        mkdir -p \$DEB_DIR/usr/share/icons/hicolor/256x256/apps
        mkdir -p \$DEB_DIR/usr/share/yep-list

        cp builddir/yep-list \$DEB_DIR/usr/bin/
        cp data/com.github.kevinadams05.yeplist.desktop \$DEB_DIR/usr/share/applications/
        cp data/yeplist.png \$DEB_DIR/usr/share/icons/hicolor/256x256/apps/com.github.kevinadams05.yeplist.png
        cp data/logo-dark.png \$DEB_DIR/usr/share/yep-list/
        cp data/logo-light.png \$DEB_DIR/usr/share/yep-list/
        cp data/CHANGELOG.md \$DEB_DIR/usr/share/yep-list/

        mkdir -p \$DEB_DIR/DEBIAN
        cat > \$DEB_DIR/DEBIAN/control <<CTRL
Package: yeplist
Version: $version
Section: utils
Priority: optional
Architecture: amd64
Depends: libgtk-4-1, libadwaita-1-0, libsoup-3.0-0, libjson-glib-1.0-0
Maintainer: Kevin Adams
Description: YepList - A simple cross-platform to-do list app
 GTK4/libadwaita client for the YepList task management system.
 Syncs with a central REST API server.
CTRL

        dpkg-deb --build \$DEB_DIR /tmp/yeplist_${version}_amd64.deb
DEBEOF
    ok "Built yeplist_${version}_amd64.deb"

    log "5/6 Fetching binary and .deb from $HOST..."
    local local_bin="/tmp/yep-list"
    local local_deb="/tmp/yeplist_${version}_amd64.deb"
    scp -q "${USER}@${HOST}:${LINUX_REMOTE_DIR}/builddir/yep-list" "$local_bin"
    scp -q "${USER}@${HOST}:/tmp/yeplist_${version}_amd64.deb" "$local_deb"
    ok "Downloaded binary and .deb"

    log "6/6 Copying to network share ($SHARE_DIR)..."
    mkdir -p "$SHARE_DIR"
    cp "$local_bin" "$SHARE_DIR/yep-list"
    cp "$local_deb" "$SHARE_DIR/yeplist_${version}_amd64.deb"
    rm -f "$local_bin" "$local_deb"
    ok "Copied yep-list and .deb to $SHARE_DIR"

    echo ""
    log "Install on other machines:"
    log "  sudo dpkg -i $SHARE_DIR/yeplist_${version}_amd64.deb"
    log "  sudo apt-get -f install   # if missing dependencies"
    log ""
    log "Run with: yep-list --server http://<server>:5000"
}

deploy_android() {
    if [ ! -f "$ANDROID_SRC_DIR/gradlew" ]; then
        err "Android client source not found at $ANDROID_SRC_DIR"
    fi

    log "1/2 Building Android APK..."
    # Use Java directly to avoid gradlew bash/bat quoting issues on Git Bash
    local gradle_java_home
    gradle_java_home=$(grep 'org.gradle.java.home' "$ANDROID_SRC_DIR/gradle.properties" | cut -d= -f2 | sed 's/\\\\*/\//g' | xargs)
    "$gradle_java_home/bin/java" -Xmx64m -Xms64m \
        -classpath "$ANDROID_SRC_DIR/gradle/wrapper/gradle-wrapper.jar" \
        org.gradle.wrapper.GradleWrapperMain \
        -p "$ANDROID_SRC_DIR" \
        assembleDebug --no-daemon -q
    ok "Build succeeded"

    local apk="$ANDROID_SRC_DIR/app/build/outputs/apk/debug/YepList.apk"
    if [ ! -f "$apk" ]; then
        err "APK not found at $apk"
    fi

    log "2/2 Copying APK to network share ($SHARE_DIR)..."
    mkdir -p "$SHARE_DIR"
    cp "$apk" "$SHARE_DIR/YepList.apk"
    ok "Copied YepList.apk to $SHARE_DIR ($(du -h "$apk" | cut -f1))"
}

deploy_sql() {
    local sqlfile=$1
    if [ ! -f "$sqlfile" ]; then
        err "SQL file not found: $sqlfile"
    fi
    log "Running $sqlfile on $HOST..."
    ssh "${USER}@${HOST}" "mysql -u yepapp -p yeplist" < "$sqlfile"
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
    echo "Usage: $0 api|linux|android|sql <file>|status"
    echo ""
    echo "  api       Publish and deploy API to $HOST"
    echo "  linux     Build and install Linux client on $HOST"
    echo "  android   Build APK and copy to $SHARE_DIR"
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
    android)
        deploy_android
        ;;
    sql)
        [ $# -lt 2 ] && err "Usage: $0 sql <file.sql>"
        deploy_sql "$2"
        ;;
    status)
        check_status
        ;;
    *)
        err "Unknown command: $1. Use api|linux|android|sql|status"
        ;;
esac

echo ""
ok "Done!"
