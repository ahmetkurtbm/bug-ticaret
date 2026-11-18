# hazirla.sh ve baslat.sh'nin ortak yardımcıları.

# Codespace açılırken Docker birkaç saniye geç hazır olur; ondan önce yapılan her şey boşa gider.
docker_bekle() {
  for _ in $(seq 1 60); do
    docker info >/dev/null 2>&1 && return 0
    sleep 2
  done
  echo "Docker hazır olmadı; sol alttaki Codespaces menüsünden Rebuild Container deneyin." >&2
  return 1
}

# Altyapı ve siteler Docker'da; backend VS Code'da.
DOCKER_SERVISLERI="postgres redis rabbitmq mailpit storefront seller admin"
