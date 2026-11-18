#!/usr/bin/env bash
# Provisionamento de rede da VM: hostname, IP estático, DNS e firewall (ufw).
# Idempotente: pode rodar de novo sem quebrar nada.
# Uso: sudo IFACE=ens33 IP=192.168.56.10/24 GATEWAY=192.168.56.1 ./01_rede.sh
set -euo pipefail
source "$(dirname "$0")/comum.sh"
exigir_root

HOSTNAME_VM="${HOSTNAME_VM:-sampaia-vm}"
IFACE="${IFACE:-$(ip -o -4 route show to default | awk '{print $5}' | head -1)}"
IP="${IP:-}"                      # vazio = mantém DHCP
GATEWAY="${GATEWAY:-}"
DNS="${DNS:-1.1.1.1,8.8.8.8}"

# Fuso de São Paulo antes de tudo: logs e auditoria saem no horário local, e os
# lembretes calculam "vence amanhã" pela data local.
timedatectl set-timezone America/Sao_Paulo
log "fuso horário: $(timedatectl show -p Timezone --value)"

auditar "inicio iface=$IFACE ip=${IP:-dhcp}"

# 1. Hostname
if [[ "$(hostnamectl --static)" != "$HOSTNAME_VM" ]]; then
  hostnamectl set-hostname "$HOSTNAME_VM"
  log "hostname definido: $HOSTNAME_VM"
fi
grep -q "127.0.1.1 $HOSTNAME_VM" /etc/hosts || echo "127.0.1.1 $HOSTNAME_VM" >> /etc/hosts

# 2. IP estático e DNS (netplan). Sem IP informado, só ajusta o DNS.
NETPLAN=/etc/netplan/60-sampaia.yaml
if [[ -n "$IP" ]]; then
  [[ -n "$GATEWAY" ]] || { log "informe GATEWAY junto com IP"; exit 1; }
  cat > "$NETPLAN" <<YAML
network:
  version: 2
  ethernets:
    $IFACE:
      dhcp4: false
      addresses: [$IP]
      routes:
        - to: default
          via: $GATEWAY
      nameservers:
        addresses: [$DNS]
YAML
else
  cat > "$NETPLAN" <<YAML
network:
  version: 2
  ethernets:
    $IFACE:
      dhcp4: true
      nameservers:
        addresses: [$DNS]
YAML
fi
chmod 600 "$NETPLAN"
netplan generate
netplan apply
log "rede aplicada em $IFACE (${IP:-dhcp}), DNS $DNS"

# 3. Firewall: só SSH e as duas APIs públicas. O serviço de ML (8010) fica só em localhost.
apt-get install -y -qq ufw >/dev/null
ufw default deny incoming >/dev/null
ufw default allow outgoing >/dev/null
for porta in 22/tcp 5009/tcp 5010/tcp; do ufw allow "$porta" >/dev/null; done
ufw --force enable >/dev/null
ufw status verbose
log "firewall ativo"

auditar "fim ok"
