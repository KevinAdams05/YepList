#!/bin/bash
set -euo pipefail

# ============================================================================
# YepList — Server Provisioning Script
# ============================================================================
# Run this script as a user with sudo privileges on a fresh Ubuntu/Mint
# installation to prepare it as the YepList API + MySQL server.
#
# Usage:
#   chmod +x provision-server.sh
#   sudo ./provision-server.sh
#
# What this script does:
#   1. Installs .NET 10 runtime
#   2. Installs and configures MySQL 8.0
#   3. Creates the yeplist database and yepapp MySQL user
#   4. Runs the schema init script
#   5. Creates the systemd service for the API
#   6. Opens firewall ports (SSH 22, API 5000, MySQL 3306)
#   7. Verifies everything is running
# ============================================================================

# ── Configuration ──────────────────────────────────────────────
API_PATH="/opt/yeplist"
API_SERVICE="yeplist-api"
MYSQL_DB="yeplist"
MYSQL_USER="yepapp"

# ── Prompt for passwords ──────────────────────────────────────
echo "============================================"
echo " YepList — Server Provisioning"
echo "============================================"
echo ""

read -sp "Enter password for MySQL user '$MYSQL_USER': " MYSQL_PASS
echo ""
read -sp "Set MySQL root password: " MYSQL_ROOT_PASS
echo ""
echo ""

# ── Pre-flight checks ─────────────────────────────────────────
if [ "$(id -u)" -ne 0 ]; then
    echo "ERROR: This script must be run as root (use sudo)."
    exit 1
fi

# ── .NET Runtime ──────────────────────────────────────────────
echo "[1/7] Installing .NET 10 runtime..."

# Add Microsoft package repository
if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
    wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    rm /tmp/packages-microsoft-prod.deb
fi

apt-get update -qq
apt-get install -y -qq aspnetcore-runtime-10.0 > /dev/null
echo "  .NET runtime installed: $(dotnet --list-runtimes | grep AspNetCore | tail -1)"

# ── MySQL Installation ────────────────────────────────────────
echo "[2/7] Installing MySQL 8.0..."
export DEBIAN_FRONTEND=noninteractive

debconf-set-selections <<< "mysql-server mysql-server/root_password password $MYSQL_ROOT_PASS"
debconf-set-selections <<< "mysql-server mysql-server/root_password_again password $MYSQL_ROOT_PASS"

apt-get install -y -qq mysql-server > /dev/null
systemctl enable mysql
systemctl start mysql
echo "  MySQL installed and running."

# ── MySQL Database + User ─────────────────────────────────────
echo "[3/7] Creating database and MySQL user..."

mysql -u root -p"$MYSQL_ROOT_PASS" <<EOSQL
CREATE DATABASE IF NOT EXISTS \`$MYSQL_DB\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '$MYSQL_USER'@'localhost' IDENTIFIED BY '$MYSQL_PASS';
GRANT ALL PRIVILEGES ON \`$MYSQL_DB\`.* TO '$MYSQL_USER'@'localhost';
FLUSH PRIVILEGES;
EOSQL

echo "  Database '$MYSQL_DB' created."
echo "  User '$MYSQL_USER' granted full access."

# ── MySQL Remote Access (optional, for dev tools) ─────────────
echo "[4/7] Configuring MySQL..."

MYSQL_CONF=""
for f in /etc/mysql/mysql.conf.d/mysqld.cnf /etc/mysql/my.cnf /etc/my.cnf; do
    if [ -f "$f" ]; then
        MYSQL_CONF="$f"
        break
    fi
done

if [ -n "$MYSQL_CONF" ]; then
    if grep -q "^bind-address" "$MYSQL_CONF"; then
        sed -i 's/^bind-address\s*=.*/bind-address = 0.0.0.0/' "$MYSQL_CONF"
    elif grep -q "^#.*bind-address" "$MYSQL_CONF"; then
        sed -i 's/^#.*bind-address.*/bind-address = 0.0.0.0/' "$MYSQL_CONF"
    else
        echo "bind-address = 0.0.0.0" >> "$MYSQL_CONF"
    fi
    echo "  bind-address set to 0.0.0.0"

    # Also create a remote user for dev tools
    mysql -u root -p"$MYSQL_ROOT_PASS" <<EOSQL2
CREATE USER IF NOT EXISTS '$MYSQL_USER'@'%' IDENTIFIED BY '$MYSQL_PASS';
GRANT ALL PRIVILEGES ON \`$MYSQL_DB\`.* TO '$MYSQL_USER'@'%';
FLUSH PRIVILEGES;
EOSQL2
    echo "  Remote MySQL access configured."
fi

systemctl restart mysql

# ── Run Schema ────────────────────────────────────────────────
echo "[5/7] Creating application directory..."
mkdir -p "$API_PATH"

# ── Systemd Service ───────────────────────────────────────────
echo "[6/7] Creating systemd service..."

cat > /etc/systemd/system/$API_SERVICE.service <<EOF
[Unit]
Description=YepList REST API
After=network.target mysql.service

[Service]
Type=exec
WorkingDirectory=$API_PATH
ExecStart=$API_PATH/ToDoList.Api
Restart=always
RestartSec=5
SyslogIdentifier=yeplist-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable $API_SERVICE
echo "  Service '$API_SERVICE' created and enabled."

# ── Firewall ──────────────────────────────────────────────────
echo "[7/7] Configuring firewall..."

if ! command -v ufw &>/dev/null; then
    apt-get install -y -qq ufw > /dev/null
fi

ufw allow 22/tcp comment "SSH"         > /dev/null
ufw allow 5000/tcp comment "YepList API" > /dev/null
ufw allow 3306/tcp comment "MySQL"     > /dev/null

if ! ufw status | grep -q "Status: active"; then
    echo "y" | ufw enable > /dev/null
fi

echo "  Ports 22 (SSH), 5000 (API), 3306 (MySQL) open."

# ── Verify ────────────────────────────────────────────────────
echo ""
echo "Verifying services..."
echo ""

PASS=true

check_service() {
    if systemctl is-active --quiet "$1"; then
        echo "  ✓ $1 is running"
    else
        echo "  ✗ $1 is NOT running"
        PASS=false
    fi
}

check_service mysql

if mysql -u "$MYSQL_USER" -p"$MYSQL_PASS" -e "SELECT 1" &>/dev/null; then
    echo "  ✓ MySQL user '$MYSQL_USER' can authenticate"
else
    echo "  ✗ MySQL user '$MYSQL_USER' authentication failed"
    PASS=false
fi

if mysql -u "$MYSQL_USER" -p"$MYSQL_PASS" -e "USE $MYSQL_DB" &>/dev/null; then
    echo "  ✓ Database '$MYSQL_DB' exists"
else
    echo "  ✗ Database '$MYSQL_DB' not found"
    PASS=false
fi

echo ""
SERVER_IP=$(hostname -I | awk '{print $1}')

if [ "$PASS" = true ]; then
    echo "============================================"
    echo " Provisioning complete!"
    echo "============================================"
    echo ""
    echo " Server IP:      $SERVER_IP"
    echo " API URL:        http://$SERVER_IP:5000"
    echo " MySQL:          mysql -h $SERVER_IP -u $MYSQL_USER -p"
    echo " Database:        $MYSQL_DB"
    echo ""
    echo " Next steps:"
    echo "   1. Run the schema:  mysql -u $MYSQL_USER -p $MYSQL_DB < backend/src/ToDoList.Data/Schema/init.sql"
    echo "   2. Deploy the API:  ./scripts/deploy.sh api"
    echo "   3. Create appsettings.Production.json in $API_PATH with:"
    echo "        {\"ConnectionStrings\":{\"Default\":\"Server=localhost;Database=$MYSQL_DB;User=$MYSQL_USER;Password=<password>\"}}"
    echo "   4. Test: curl http://$SERVER_IP:5000/api/lists"
    echo ""
else
    echo "============================================"
    echo " Provisioning completed with warnings."
    echo " Review the items marked ✗ above."
    echo "============================================"
fi
