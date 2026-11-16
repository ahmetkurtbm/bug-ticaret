#!/usr/bin/env bash
# Codespace her açıldığında çalışır (postStartCommand): durdurulup yeniden açılınca sistem de açılır.
set -uo pipefail
cd "$(dirname "$0")/.."

docker compose --profile app up -d >/dev/null 2>&1 || true

# Siteler API'ye ve ödeme sayfasına tarayıcıdan, ayrı adreslerden ulaşır. Codespaces portları varsayılan
# olarak gizlidir (GitHub girişi ister) ve sitenin arka plan istekleri bu girişi taşıyamaz; bu yüzden
# API ve ödeme portları herkese açık yapılır. İçerik yalnızca kurgusal örnek veridir.
if [ -n "${CODESPACE_NAME:-}" ] && command -v gh >/dev/null 2>&1; then
  for _ in $(seq 1 30); do
    if gh codespace ports visibility 5080:public 5090:public -c "$CODESPACE_NAME" >/dev/null 2>&1; then
      echo "✔ API ve ödeme portları açık"
      break
    fi
    sleep 5
  done
fi
