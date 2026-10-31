#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
LUBAN="$SCRIPT_DIR/LubanDll/Luban.dll"
CONF="$SCRIPT_DIR/DataTables/luban.conf"
CLIENT_ASSETS="$REPO_ROOT/Client/GameTopRouge/Assets/AAAGame"
SERVER_CONFIG="$REPO_ROOT/Server/Hotfix/Config"

export DOTNET_ROLL_FORWARD=Major
cd "$SCRIPT_DIR"

echo "Generating client Luban tables..."
dotnet "$LUBAN" -t client -c cs-bin -d bin --conf "$CONF" \
    -x "outputCodeDir=$CLIENT_ASSETS/ScriptsHotfix/DataTable" \
    -x "outputDataDir=$CLIENT_ASSETS/DataTable"

echo "Generating server Luban tables..."
dotnet "$LUBAN" -t server -c cs-bin -d bin --conf "$CONF" \
    -x "outputCodeDir=$SERVER_CONFIG/Code" \
    -x "outputDataDir=$SERVER_CONFIG/Data"

python3 "$SCRIPT_DIR/validate_dialogue.py"
python3 "$SCRIPT_DIR/validate_quest.py"
echo "Luban client and server tables generated successfully."
