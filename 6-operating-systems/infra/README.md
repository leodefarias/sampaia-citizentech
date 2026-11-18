# Operating Systems — provisionamento da VM

Uma VM Ubuntu (24.04 ou mais nova) provisionada só por scripts Bash sobe as duas APIs .NET, o serviço de previsão em Python e o Node-RED, que liga o ESP32 à API de Lembretes por MQTT.

| Script / arquivo | O que faz | Critério |
|---|---|---|
| `01_rede.sh` | hostname, IP estático ou DHCP (netplan), DNS, firewall `ufw` (22, 5009, 5010) | Provisionamento de infra (2) |
| `02_app.sh` | instala .NET 10 e Python, cria usuário `sampaia` sem login, grava `/etc/sampaia/sampaia.env` (chave do LLM, Oracle), publica as APIs, aplica migrações, instala e liga as units | Provisionamento da aplicação (2) |
| `03_nodered.sh` | instala Node.js e Node-RED para o usuário `sampaia`, grava `MQTT_BROKER`, `MQTT_PORTA`, `SAMPAIA_DISPOSITIVO` e `LEMBRETES_URL` no `sampaia.env`, copia `node-red/flows.json` e liga a unit `sampaia-nodered` | Provisionamento da aplicação (2) |
| `monitor.sh` | CPU, memória, disco e saúde das APIs (e do Node-RED, se instalado), com alerta em `/var/log/sampaia/alertas.log` e no journal | Monitoramento com alertas (2) |
| `systemd/sampaia-monitor.service` + `.timer` | monitor em loop com `Restart=always`; o timer sobe de novo se alguém parar o serviço | Serviço tolerante a falhas (1) |
| `comum.sh` | toda execução vai para `/var/log/sampaia/<script>.log`; quem rodou, quando e resultado vão para `/var/log/sampaia/auditoria.log` | Logs de auditoria (1) |
| `teste_falha.sh` | `kill -9` na API do Agente e no monitor e mostra o systemd subindo os dois de novo | Funcionamento completo (2) |

Todos os scripts usam `set -euo pipefail`, são idempotentes (rodar duas vezes não quebra nada) e passaram no `shellcheck`.

**Vídeo:** https://youtu.be/ULz_PL6kzJI ([`videos/os-provisionamento-vm.mp4`](../videos/os-provisionamento-vm.mp4)), este roteiro rodando numa VM Ubuntu 24.04 limpa (log completo em [`evidencias/provisionamento-vm.txt`](evidencias/provisionamento-vm.txt)).

## Passo a passo (roteiro do vídeo de até 7 min)

```bash
# na VM, com o repositório clonado
cd sampaia
sudo IP=192.168.56.10/24 GATEWAY=192.168.56.1 ./infra/01_rede.sh      # sem IP = DHCP
sudo ANTHROPIC_API_KEY=... ORACLE_CONN='User Id=...;Password=...;Data Source=...' ./infra/02_app.sh
curl http://localhost:5009/api/servicos                                  # API respondendo
sudo ./infra/03_nodered.sh                                               # ponte MQTT do ESP32 (sem ngrok)
sudo ./infra/teste_falha.sh                                              # kill -9 e systemd reiniciando
sudo tail /var/log/sampaia/auditoria.log /var/log/sampaia/alertas.log
journalctl -t sampaia-monitor -n 20
```

Sem `ORACLE_CONN`, as APIs usam SQLite em `/opt/sampaia/dados`. Sem `ANTHROPIC_API_KEY`, usam o LLM simulado. O modelo de ML (`ml/modelo.joblib`) precisa existir no clone; sem ele a API do Agente responde com estimativa.

## Serviços

| Unit | Porta | Observação |
|---|---|---|
| `sampaia-agente` | 5009 | `Restart=always`, usuário `sampaia`, `ProtectSystem=strict` |
| `sampaia-lembretes` | 5010 | idem |
| `sampaia-previsao` | 8010 (só localhost) | uvicorn + FastAPI |
| `sampaia-nodered` | 1880 (só localhost) | fluxo MQTT ⇄ API de Lembretes; editor via `ssh -L 1880:localhost:1880` |
| `sampaia-monitor` (+ timer) | — | alerta se CPU > 85%, memória > 90%, disco > 85% ou API sem resposta |

## ESP32 por MQTT e Node-RED (sem ngrok)

O Wokwi roda na nuvem e não alcança a VM. Antes, isso exigia um túnel `ngrok` para a porta 5010. Agora os dois lados só abrem conexões de **saída** para um broker MQTT, e nenhuma porta nova é aberta no firewall:

```
ESP32 (Wokwi) ⇄ broker MQTT ⇄ Node-RED (VM) → http://localhost:5010 (API de Lembretes)
```

| Tópico | Quem publica | Conteúdo |
|---|---|---|
| `sampaia/<DISPOSITIVO>/pedir` | ESP32 | `1`: manda o próximo lembrete |
| `sampaia/<DISPOSITIVO>/concluir` | ESP32 | `{"id": N}` (botão "Já paguei") |
| `sampaia/<DISPOSITIVO>/lembrete` | Node-RED (retido) | `{id, titulo, dias, alerta}` ou `{"id": -1}` |
| `sampaia/<DISPOSITIVO>/status` | ESP32 (last will) | `online` / `offline` |

O fluxo (`node-red/flows.json`) consulta a API ao iniciar, a cada 30 min e a cada pedido. No "Já paguei", ele chama `POST /concluir` e publica o próximo lembrete. O padrão é o broker público `broker.hivemq.com:1883`, por isso o código `SAMPAIA_DISPOSITIVO` (na demo, `casa-7f3k9q`, o mesmo do `iot/sketch.ino`) funciona como senha dos tópicos. O Node-RED descarta mensagens de qualquer outro dispositivo, então um estranho no broker não consegue marcar uma conta como paga. As mensagens trazem só o nome da conta e os dias, nada pessoal. Para uso real, troque o código por um só seu (nos dois lados) e use um broker próprio com usuário e senha (`MQTT_BROKER`, `MQTT_PORTA`).

## Evidências

Depois de rodar o passo a passo na VM, salve as saídas em `infra/evidencias/` (texto puro, sem senha nem chave):

| Arquivo | Como gerar | Prova |
|---|---|---|
| `01_rede.txt` | `sudo cat /var/log/sampaia/01_rede.log` | rede, DNS, hostname |
| `02_app.txt` | `sudo cat /var/log/sampaia/02_app.log` | provisionamento da aplicação |
| `servicos.txt` | `systemctl status 'sampaia-*' --no-pager` | units ativas |
| `nodered.txt` | `sudo cat /var/log/sampaia/03_nodered.log; systemctl status sampaia-nodered --no-pager` | ponte MQTT ativa |
| `teste_falha.txt` | `sudo ./infra/teste_falha.sh 2>&1 \| tee infra/evidencias/teste_falha.txt` | systemd reiniciando após `kill -9` |
| `auditoria.txt` | `sudo cat /var/log/sampaia/auditoria.log` | quem rodou o quê e quando |
| `alertas.txt` | `sudo LIMITE_MEM=1 LIMITE_DISCO=1 /usr/local/bin/sampaia-monitor` (uma rodada com limites baixos, força os alertas de memória e disco), depois `sudo cat /var/log/sampaia/alertas.log; journalctl -t sampaia-monitor -n 50` | alertas de CPU, memória e disco |

Confira `02_app.txt` antes de commitar: não pode ter a chave do LLM nem a senha do Oracle.
