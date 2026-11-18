#!/usr/bin/env bash
# Provisionamento da aplicação: .NET, Python, usuário de serviço, variáveis de ambiente,
# build das APIs, migrações e units systemd das 3 APIs (Agente, Lembretes, Previsão).
# Idempotente. Rode a partir de um clone do repositório:
#   sudo ANTHROPIC_API_KEY=... ORACLE_CONN='User Id=...;Password=...;Data Source=...' ./infra/02_app.sh
set -euo pipefail
source "$(dirname "$0")/comum.sh"
exigir_root

REPO="$(cd "$(dirname "$0")/.." && pwd)"
DESTINO=/opt/sampaia
USUARIO=sampaia
ENV_FILE=/etc/sampaia/sampaia.env

auditar "inicio repo=$REPO"

# 1. Pacotes: .NET 10 (repositório backports do Ubuntu) e Python
if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  log "instalando .NET 10"
  apt-get update -qq
  apt-get install -y -qq software-properties-common >/dev/null
  add-apt-repository -y ppa:dotnet/backports >/dev/null
  apt-get update -qq
  apt-get install -y -qq dotnet-sdk-10.0 >/dev/null
fi
apt-get install -y -qq python3 python3-venv rsync curl >/dev/null
log "dotnet $(dotnet --version), $(python3 --version)"

# 2. Usuário de serviço sem login
id "$USUARIO" &>/dev/null || useradd --system --home "$DESTINO" --shell /usr/sbin/nologin "$USUARIO"

# 3. Variáveis de ambiente (segredos só aqui, nunca no git). Mantém valores já gravados.
mkdir -p /etc/sampaia
touch "$ENV_FILE"
definir() { # definir CHAVE VALOR: grava só se veio valor
  [[ -z "$2" ]] && return
  sed -i "/^$1=/d" "$ENV_FILE"
  echo "$1=$2" >> "$ENV_FILE"
}
definir ANTHROPIC_API_KEY "${ANTHROPIC_API_KEY:-}"
definir ConnectionStrings__Oracle "${ORACLE_CONN:-}"
grep -q '^Lembretes__Url=' "$ENV_FILE" || echo "Lembretes__Url=http://localhost:5010" >> "$ENV_FILE"
grep -q '^Previsao__Url=' "$ENV_FILE" || echo "Previsao__Url=http://localhost:8010" >> "$ENV_FILE"
grep -q '^ASPNETCORE_ENVIRONMENT=' "$ENV_FILE" || echo "ASPNETCORE_ENVIRONMENT=Production" >> "$ENV_FILE"
chown root:"$USUARIO" "$ENV_FILE"
chmod 640 "$ENV_FILE"
log "variáveis em $ENV_FILE (chaves: $(cut -d= -f1 "$ENV_FILE" | paste -sd, -))"

# 4. Build das APIs .NET e cópia do serviço de ML
mkdir -p "$DESTINO"/{agente,lembretes,ml,dados}
dotnet publish "$REPO/backend/Agente.Api" -c Release -o "$DESTINO/agente" --nologo -v q
dotnet publish "$REPO/backend/Lembretes.Api" -c Release -o "$DESTINO/lembretes" --nologo -v q
rsync -a --delete --exclude .venv --exclude 'dados/*.csv' "$REPO/ml/" "$DESTINO/ml/"
[[ -f "$DESTINO/ml/modelo.joblib" ]] || log "AVISO: ml/modelo.joblib não encontrado; execute o notebook antes (a API usa estimativa até lá)"
python3 -m venv "$DESTINO/ml/.venv"
"$DESTINO/ml/.venv/bin/pip" install -q -r "$DESTINO/ml/requirements.txt"
chown -R "$USUARIO":"$USUARIO" "$DESTINO"
log "build publicado em $DESTINO"

# 5. Migrações (só com Oracle configurado; sem Oracle as APIs usam SQLite em $DESTINO/dados)
if grep -q '^ConnectionStrings__Oracle=' "$ENV_FILE"; then
  export "$(grep '^ConnectionStrings__Oracle=' "$ENV_FILE")"
  (cd "$REPO/backend" && dotnet tool restore >/dev/null \
    && dotnet ef database update -p Agente.Infra -s Agente.Infra \
    && dotnet ef database update -p Lembretes.Api -s Lembretes.Api)
  log "migrações aplicadas no Oracle"
else
  log "sem Oracle: SQLite local em $DESTINO/dados"
fi

# 6. Units systemd (Restart=always) e monitor
install -m 644 "$REPO"/infra/systemd/*.service "$REPO"/infra/systemd/*.timer /etc/systemd/system/
install -m 755 "$REPO/infra/monitor.sh" /usr/local/bin/sampaia-monitor
install -m 644 "$REPO/infra/comum.sh" /usr/local/bin/comum.sh
systemctl daemon-reload
for u in sampaia-previsao sampaia-lembretes sampaia-agente sampaia-monitor; do
  systemctl enable --now "$u.service" >/dev/null
  systemctl restart "$u.service"
done
systemctl enable --now sampaia-monitor.timer >/dev/null

# 7. Conferência
sleep 5
for url in http://localhost:5009/api/servicos http://localhost:5010/api/lembretes; do
  log "$url -> HTTP $(curl -s -o /dev/null -w '%{http_code}' "$url")"
done
systemctl --no-pager --lines=0 status sampaia-agente sampaia-lembretes sampaia-previsao sampaia-monitor | grep -E '●|Active' || true

auditar "fim ok"
