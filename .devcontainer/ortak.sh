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

# Codespace bilgileri (adı, port alan adı, GitHub token'ı). Açılış komutlarında ortamda olur; SSH ya da
# arka plan işlerinde olmayabilir, o zaman Codespaces'in sakladığı dosyadan okunur.
codespace_ortami() {
  local f=/workspaces/.codespaces/shared/.env
  if [ -z "${CODESPACE_NAME:-}" ] && [ -f "$f" ]; then
    while IFS='=' read -r k v; do
      case "$k" in CODESPACE_NAME|GITHUB_TOKEN|GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN) export "$k=$v" ;; esac
    done < "$f"
  fi
  [ -n "${GITHUB_TOKEN:-}" ] && [ -z "${GH_TOKEN:-}" ] && export GH_TOKEN="$GITHUB_TOKEN"
  return 0
}

# Altyapı ve siteler Docker'da; backend VS Code'da.
DOCKER_SERVISLERI="postgres redis rabbitmq mailpit storefront seller admin"
