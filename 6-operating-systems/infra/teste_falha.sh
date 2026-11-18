#!/usr/bin/env bash
# Teste de falha para o vídeo: derruba a API do Agente e o monitor com kill -9
# e mostra o systemd subindo os dois de novo (Restart=always).
set -euo pipefail
source "$(dirname "$0")/comum.sh"
exigir_root
auditar "inicio teste de falha"

resultado=""
for SERVICO in sampaia-agente sampaia-monitor; do
  pid_antes=$(systemctl show -p MainPID --value "$SERVICO")
  log "[$SERVICO] PID antes: $pid_antes"
  kill -9 "$pid_antes"
  log "[$SERVICO] kill -9 enviado; aguardando o systemd reiniciar..."

  for _ in $(seq 1 20); do
    sleep 1
    pid_depois=$(systemctl show -p MainPID --value "$SERVICO")
    if [[ "$pid_depois" != "0" && "$pid_depois" != "$pid_antes" ]]; then break; fi
  done
  log "[$SERVICO] PID depois: $pid_depois (reinícios: $(systemctl show -p NRestarts --value "$SERVICO"))"
  journalctl -u "$SERVICO" --since "-1min" --no-pager | tail -5
  resultado+=" $SERVICO:$pid_antes->$pid_depois"
done

for _ in $(seq 1 30); do curl -fs -o /dev/null http://localhost:5009/api/servicos && break; sleep 1; done
log "API do Agente respondendo: HTTP $(curl -s -o /dev/null -w '%{http_code}' http://localhost:5009/api/servicos)"
auditar "fim teste de falha$resultado"
