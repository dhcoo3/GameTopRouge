#!/bin/bash

set -euo pipefail
SCRIPT_DIR=$(cd -- "$(dirname -- "$0")" && pwd)

echo "=========================================="
echo "Fantasy Protocol Export Tool 2026.1.2000"
echo "=========================================="
echo ""

python3 "$SCRIPT_DIR/export_protocol.py"
