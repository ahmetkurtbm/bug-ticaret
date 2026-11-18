#!/usr/bin/env bash
# Codespace her açıldığında çalışır (postStartCommand): durdurulup yeniden açılınca da.
set -uo pipefail
cd "$(dirname "$0")/.."
source .devcontainer/ortak.sh

docker_bekle || exit 0
docker compose --profile app up -d $DOCKER_SERVISLERI >/dev/null 2>&1

# API ve ödeme portlarını herkese açık yap (siteler onlara tarayıcıdan, ayrı adreslerden ulaşır).
# Servisler F5'ten sonra açılır; o zamana kadar arka planda tekrar denenir.
nohup bash .devcontainer/portlari-ac.sh >/tmp/portlari-ac.log 2>&1 &
