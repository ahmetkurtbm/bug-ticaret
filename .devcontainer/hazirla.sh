#!/usr/bin/env bash
# Codespace ilk oluşturulurken bir kez çalışır (postCreateCommand).
set -euo pipefail
cd "$(dirname "$0")/.."
source .devcontainer/ortak.sh

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
fi

echo "▶ .NET araçları ve frontend bağımlılıkları kuruluyor"
(cd backend && dotnet tool restore >/dev/null && dotnet build Tezgah.slnx -v q >/dev/null)
(cd frontend && npm ci --no-audit --no-fund >/dev/null)

docker_bekle

echo "▶ Sistem derleniyor, veritabanı kuruluyor, örnek veri üretiliyor (birkaç dakika)"
docker compose --profile app up -d --build
for _ in $(seq 1 120); do
  docker compose logs seed 2>/dev/null | grep -q "Bitti\|zaten veri" && break
  sleep 5
done

# Backend servisleri VS Code'dan (F5) hata ayıklayıcıyla çalışır; Docker'dakiler kapatılır.
docker compose stop api payments notifications >/dev/null 2>&1 || true
echo "✔ Hazır. Backend'i başlatmak için F5 (Tüm backend). Rehber: docs/CODESPACES.md"
