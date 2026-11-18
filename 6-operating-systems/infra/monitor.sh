#!/usr/bin/env bash
# Monitoramento com alertas: CPU, memória, disco e saúde das APIs.
# Uso: monitor.sh          (uma verificação)
#      monitor.sh --loop   (verifica a cada INTERVALO segundos; é assim que o systemd roda)
# Alertas vão para /var/log/sampaia/alertas.log e para o journal (logger -t sampaia-monitor).
set -euo pipefail
source "$(dirname "$(readlink -f "$0")")/comum.sh"

LIMITE_CPU="${LIMITE_CPU:-85}"      # % de uso
LIMITE_MEM="${LIMITE_MEM:-90}"      # % de uso
LIMITE_DISCO="${LIMITE_DISCO:-85}"  # % de uso da partição /
INTERVALO="${INTERVALO:-60}"
APIS="${APIS:-http://localhost:5009/api/servicos http://localhost:5010/api/lembretes}"
# Com a ponte MQTT instalada (03_nodered.sh), o Node-RED também é vigiado.
if systemctl is-enabled --quiet sampaia-nodered 2>/dev/null; then APIS="$APIS http://127.0.0.1:1880/"; fi

alertar() {
  local msg="ALERTA $*"
  log "$msg"
  echo "$(date '+%F %T') $msg" >> "$LOG_DIR/alertas.log"
  logger -t sampaia-monitor -p user.warning "$msg" 2>/dev/null || true
}

uso_cpu() { # média de 1 s a partir de /proc/stat
  local a b
  read -r _ a1 a2 a3 a4 _ < /proc/stat; a=$((a1 + a2 + a3)); local ia=$a4
  sleep 1
  read -r _ b1 b2 b3 b4 _ < /proc/stat; b=$((b1 + b2 + b3)); local ib=$b4
  echo $(( 100 * (b - a) / ((b - a) + (ib - ia) + 1) ))
}

uso_mem() { free | awk '/^Mem:/ {printf "%d", ($2 - $7) * 100 / $2}'; }
uso_disco() { df --output=pcent / | tail -1 | tr -dc '0-9'; }

verificar() {
  local cpu mem disco
  cpu=$(uso_cpu); mem=$(uso_mem); disco=$(uso_disco)
  log "cpu=${cpu}% mem=${mem}% disco=${disco}%"
  (( cpu > LIMITE_CPU )) && alertar "CPU em ${cpu}% (limite ${LIMITE_CPU}%)"
  (( mem > LIMITE_MEM )) && alertar "memória em ${mem}% (limite ${LIMITE_MEM}%)"
  (( disco > LIMITE_DISCO )) && alertar "disco em ${disco}% (limite ${LIMITE_DISCO}%)"
  for url in $APIS; do
    curl -fs -o /dev/null -m 5 "$url" || alertar "API sem resposta: $url"
  done
  return 0
}

if [[ "${1:-}" == "--loop" ]]; then
  log "monitor iniciado (intervalo ${INTERVALO}s)"
  while true; do verificar; sleep "$INTERVALO"; done
else
  verificar
fi
