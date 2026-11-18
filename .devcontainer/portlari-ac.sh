#!/usr/bin/env bash
# API (5080) ve Ödeme (5090) portları dinlemeye başlayınca onları "public" yapar. 30 dakika dener.
cd "$(dirname "$0")/.."
source .devcontainer/ortak.sh
codespace_ortami
[ -n "${CODESPACE_NAME:-}" ] && command -v gh >/dev/null 2>&1 || { echo "Codespace bilgisi yok"; exit 0; }
echo "Portlar bekleniyor: $CODESPACE_NAME"
for _ in $(seq 1 180); do
  ok=1
  for p in 5080 5090; do
    curl -fs "localhost:$p/health" >/dev/null 2>&1 || { ok=0; continue; }
    vis=$(gh codespace ports -c "$CODESPACE_NAME" --json sourcePort,visibility -q ".[] | select(.sourcePort==$p) | .visibility" 2>/dev/null)
    [ "$vis" = "public" ] || gh codespace ports visibility "$p:public" -c "$CODESPACE_NAME" >/dev/null 2>&1 || ok=0
  done
  [ $ok = 1 ] && { echo "✔ API ve ödeme portları açık"; exit 0; }
  sleep 10
done
