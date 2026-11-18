#!/usr/bin/env bash
# Ponte do ESP32 por MQTT + Node-RED (substitui o túnel ngrok).
# O ESP32 no Wokwi e o Node-RED na VM só abrem conexões de SAÍDA para o broker MQTT;
# o Node-RED fala com a API de Lembretes em localhost. Nenhuma porta nova é aberta no firewall.
# Idempotente. Rode depois do 02_app.sh:
#   sudo ./infra/03_nodered.sh   (dispositivo padrão casa-7f3k9q, o mesmo do iot/sketch.ino)
#   (MQTT_BROKER e MQTT_PORTA opcionais; padrão: broker público broker.hivemq.com:1883)
set -euo pipefail
source "$(dirname "$0")/comum.sh"
exigir_root

REPO="$(cd "$(dirname "$0")/.." && pwd)"
DESTINO=/opt/sampaia/node-red
USUARIO=sampaia
ENV_FILE=/etc/sampaia/sampaia.env
NODE_RED_VERSAO="${NODE_RED_VERSAO:-4}"

auditar "inicio"
[[ -f "$ENV_FILE" ]] || { log "rode antes o 02_app.sh (falta $ENV_FILE)"; exit 1; }

# 1. Node.js (Node-RED 4 pede Node 18 ou mais novo)
if ! command -v node >/dev/null || (( $(node -p 'process.versions.node.split(".")[0]') < 18 )); then
  log "instalando Node.js"
  apt-get update -qq
  apt-get install -y -qq nodejs npm >/dev/null
fi
log "node $(node --version)"

# 2. Variáveis: mantém o que já foi gravado. O código do dispositivo padrão é o mesmo do iot/sketch.ino.
definir_se_faltar() { # definir_se_faltar CHAVE VALOR
  grep -q "^$1=" "$ENV_FILE" || echo "$1=$2" >> "$ENV_FILE"
}
if [[ -n "${SAMPAIA_DISPOSITIVO:-}" ]]; then sed -i '/^SAMPAIA_DISPOSITIVO=/d' "$ENV_FILE"; fi
definir_se_faltar SAMPAIA_DISPOSITIVO "${SAMPAIA_DISPOSITIVO:-casa-7f3k9q}"
definir_se_faltar MQTT_BROKER "${MQTT_BROKER:-broker.hivemq.com}"
definir_se_faltar MQTT_PORTA "${MQTT_PORTA:-1883}"
definir_se_faltar LEMBRETES_URL "http://localhost:5010"
log "MQTT: $(grep '^MQTT_BROKER=' "$ENV_FILE" | cut -d= -f2):$(grep '^MQTT_PORTA=' "$ENV_FILE" | cut -d= -f2), dispositivo $(grep '^SAMPAIA_DISPOSITIVO=' "$ENV_FILE" | cut -d= -f2)"

# 3. Node-RED instalado só para o usuário de serviço, com o fluxo do repositório
mkdir -p "$DESTINO"
[[ -f "$DESTINO/package.json" ]] || echo '{"name":"sampaia-node-red","private":true}' > "$DESTINO/package.json"
if [[ ! -x "$DESTINO/node_modules/.bin/node-red" ]]; then
  log "instalando node-red@$NODE_RED_VERSAO"
  npm install --prefix "$DESTINO" --no-audit --no-fund --omit=dev "node-red@$NODE_RED_VERSAO" >/dev/null
fi
install -m 644 "$REPO/infra/node-red/flows.json" "$REPO/infra/node-red/settings.js" "$DESTINO/"
chown -R "$USUARIO":"$USUARIO" "$DESTINO"
log "node-red $(node -p "require('$DESTINO/node_modules/node-red/package.json').version")"

# 4. Unit systemd (Restart=always)
install -m 644 "$REPO/infra/systemd/sampaia-nodered.service" /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now sampaia-nodered.service >/dev/null
systemctl restart sampaia-nodered.service

# 5. Conferência: editor do Node-RED respondendo (só em localhost)
for _ in $(seq 1 30); do curl -fs -o /dev/null http://127.0.0.1:1880/ && break; sleep 2; done
log "Node-RED http://127.0.0.1:1880 -> HTTP $(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:1880/)"
systemctl --no-pager --lines=0 status sampaia-nodered | grep -E '●|Active' || true
log "o SAMPAIA_DISPOSITIVO precisa ser igual ao DISPOSITIVO do iot/sketch.ino"

auditar "fim ok"
