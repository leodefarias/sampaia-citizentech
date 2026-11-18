#!/usr/bin/env bash
# Funções comuns: log de execução e trilha de auditoria em /var/log/sampaia/.
# Carregado pelos outros scripts com: source "$(dirname "$0")/comum.sh"

LOG_DIR="${LOG_DIR:-/var/log/sampaia}"
mkdir -p "$LOG_DIR"
SCRIPT="$(basename "$0" .sh)"

# Toda saída do script vai para a tela e para /var/log/sampaia/<script>.log
exec > >(tee -a "$LOG_DIR/$SCRIPT.log") 2>&1

log() { echo "[$(date '+%F %T')] [$SCRIPT] $*"; }

# Auditoria: quem rodou, quando, de onde e com qual resultado.
auditar() {
  echo "$(date '+%F %T') usuario=${SUDO_USER:-$USER} script=$SCRIPT host=$(hostname) evento=$*" >> "$LOG_DIR/auditoria.log"
}

exigir_root() {
  if [[ $EUID -ne 0 ]]; then echo "Rode com sudo: sudo $0"; exit 1; fi
}

trap 'auditar "falhou linha=$LINENO"' ERR
