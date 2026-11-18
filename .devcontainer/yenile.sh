#!/usr/bin/env bash
# Codespaces: kod değişince siteleri (React) yeniden derler. Backend için VS Code'da Ctrl+Shift+F5 yeter.
#   bash .devcontainer/yenile.sh
set -euo pipefail
cd "$(dirname "$0")/.."
source .devcontainer/ortak.sh
docker compose --profile app up -d --build $DOCKER_SERVISLERI
echo "✔ Siteler yeniden derlendi. Backend'i VS Code'da yeniden başlatın: Ctrl+Shift+F5"
