#!/usr/bin/env bash
# git bisect run için yardımcı (Linux, macOS, Windows'ta Git Bash).
# Bisect sırasında bu dosya da commit'ten commit'e değişebileceği için önce depo dışına kopyalayın:
#   cp bolumler/16-arac-01/bisect-kontrol.sh /tmp/
#   git bisect start bolum/16-arac-01 main
#   git bisect run /tmp/bisect-kontrol.sh
#
# Her adımda: API'yi derler ve başlatır, "kontrol 16" çalıştırır, API'yi durdurur.
# Çıkış kodu: 0 = iyi, 1 = kötü, 125 = bu commit test edilemiyor (bisect atlar).
set -u
ROOT="$(git rev-parse --show-toplevel)"
LOG="${TMPDIR:-/tmp}/tezgah-bisect-api.log"

stop_api() {
  if command -v powershell >/dev/null 2>&1; then
    powershell -NoProfile -Command "Get-Process Tezgah.Api -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1
  else
    pkill -f "Tezgah.Api" >/dev/null 2>&1 || true
  fi
}

stop_api
cd "$ROOT/backend" || exit 125
dotnet build src/Tezgah.Api -v q >/dev/null 2>&1 || exit 125
dotnet build tools/Tezgah.Bolum -v q >/dev/null 2>&1 || exit 125

dotnet run --no-build --project src/Tezgah.Api --launch-profile http >"$LOG" 2>&1 &
for _ in $(seq 1 60); do
  curl -fs http://localhost:5080/health >/dev/null 2>&1 && break
  sleep 1
done
curl -fs http://localhost:5080/health >/dev/null 2>&1 || { stop_api; exit 125; }

cd "$ROOT"
dotnet run --no-build --project backend/tools/Tezgah.Bolum -- kontrol 16
result=$?
stop_api
[ "$result" -eq 0 ] && exit 0
[ "$result" -eq 1 ] && exit 1
exit 125
