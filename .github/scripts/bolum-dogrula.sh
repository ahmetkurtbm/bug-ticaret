#!/usr/bin/env bash
# Bir bölümün kontrolünü o anki koda karşı çalıştırır ve sonucu beklenenle karşılaştırır.
#   bolum-dogrula.sh <bölüm no> <beklenen: gecer|kalir>
# main'de "gecer", bölüm dalında "kalir" beklenir. GitHub Actions'ta .github/workflows/bolumler.yml çağırır.
set -uo pipefail
NO="$1"; EXPECT="$2"
ROOT="$(git rev-parse --show-toplevel)"; cd "$ROOT"
LOGS="${RUNNER_TEMP:-/tmp}/bolum-$NO"; mkdir -p "$LOGS"

stop_services() {
  pkill -f "Tezgah.Api.dll|Tezgah.Payments.dll|Tezgah.Notifications.dll" 2>/dev/null || true
  pkill -f "dotnet run --no-build --project backend/src/" 2>/dev/null || true
  sleep 2
}
wait_http() { for _ in $(seq 1 90); do curl -fs "$1" >/dev/null 2>&1 && return 0; sleep 2; done; echo "Yanıt yok: $1"; return 1; }

stop_services
docker compose --profile app down >/dev/null 2>&1 || true

case "$NO" in
  1|3|29)
    # Bu bölümler sistemin tamamen container'da çalışmasını ister.
    docker compose --profile app up -d --build
    if wait_http http://localhost:5080/health; then
      for _ in $(seq 1 90); do docker compose logs seed 2>/dev/null | grep -q "Bitti\|zaten veri" && break; sleep 3; done
    else
      docker compose logs api | tail -50
      # Bölüm 29'da API'nin açılmaması hatanın kendisidir; sonucu kontrol aracı söyler.
      [ "$NO" = 29 ] || exit 1
    fi
    ;;
  *)
    docker compose up -d postgres redis rabbitmq mailpit
    for _ in $(seq 1 60); do docker compose exec -T postgres pg_isready -U tezgah -h 127.0.0.1 >/dev/null 2>&1 && break; sleep 2; done
    (cd backend && dotnet tool restore >/dev/null && dotnet build Tezgah.slnx -v q --nologo) || exit 1
    # Dalın migration'ları (ör. bölüm 17) API açılırken uygulanır.
    Database__MigrateOnStartup=true nohup dotnet run --no-build --project backend/src/Tezgah.Api --launch-profile http >"$LOGS/api.log" 2>&1 &
    wait_http http://localhost:5080/health || { tail -50 "$LOGS/api.log"; exit 1; }
    if [ ! -f "$LOGS/../veri-hazir-$NO" ]; then
      dotnet run --no-build --project backend/tools/Tezgah.DataGen -- --profile small --reset >"$LOGS/datagen.log" 2>&1 || { tail -30 "$LOGS/datagen.log"; exit 1; }
      touch "$LOGS/../veri-hazir-$NO"
    fi
    nohup dotnet run --no-build --project backend/src/Tezgah.Payments --launch-profile http >"$LOGS/payments.log" 2>&1 &
    nohup dotnet run --no-build --project backend/src/Tezgah.Notifications --launch-profile http >"$LOGS/notifications.log" 2>&1 &
    wait_http http://localhost:5090/health || { tail -50 "$LOGS/payments.log"; exit 1; }
    wait_http http://localhost:5095/health || { tail -50 "$LOGS/notifications.log"; exit 1; }
    ;;
esac

[ -d frontend/node_modules ] || (cd frontend && npm ci --no-audit --no-fund >/dev/null)
dotnet build backend/tools/Tezgah.Bolum -v q --nologo >/dev/null
dotnet run --no-build --project backend/tools/Tezgah.Bolum -- kontrol "$NO"
code=$?
stop_services

case "$EXPECT:$code" in
  gecer:0) echo "✔ Bölüm $NO: kontrol beklendiği gibi geçti"; exit 0 ;;
  kalir:1) echo "✔ Bölüm $NO: kontrol beklendiği gibi hatayı yakaladı"; exit 0 ;;
  *) echo "✘ Bölüm $NO: beklenen '$EXPECT', kontrol çıkış kodu $code"; exit 1 ;;
esac
