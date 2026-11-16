#!/usr/bin/env bash
# Codespace ilk oluşturulurken bir kez çalışır (postCreateCommand).
set -euo pipefail
cd "$(dirname "$0")/.."

echo "▶ Adresler ayarlanıyor"
if [ -n "${CODESPACE_NAME:-}" ]; then
  # Codespaces'te her port kendi adresini alır: https://<codespace>-<port>.app.github.dev
  domain="${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
  url() { echo "https://${CODESPACE_NAME}-$1.${domain}"; }
  cat > .env <<EOF
# .devcontainer/hazirla.sh yazdı; bu Codespace'e özel. Silinirse yerel adresler (localhost) kullanılır.
PUBLIC_API_URL=$(url 5080)
PUBLIC_PAYMENTS_URL=$(url 5090)
PUBLIC_STOREFRONT_URL=$(url 5173)
PUBLIC_SELLER_URL=$(url 5174)
PUBLIC_ADMIN_URL=$(url 5175)
EOF
  cat .env
else
  echo "  Codespaces dışında çalışıyor; yerel adresler (localhost) kullanılacak."
fi

echo "▶ .NET araçları ve frontend bağımlılıkları kuruluyor"
(cd backend && dotnet tool restore >/dev/null)
(cd frontend && npm ci --no-audit --no-fund >/dev/null)

echo "▶ Sistem derleniyor ve örnek veri üretiliyor (ilk seferde birkaç dakika sürer)"
docker compose --profile app up -d --build

echo "✔ Hazır. Rehber: docs/CODESPACES.md"
