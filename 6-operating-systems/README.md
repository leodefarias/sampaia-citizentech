# SampaIA — Operating Systems

Infraestrutura Linux da SampaIA: uma VM **Ubuntu 24.04** provisionada só por scripts Bash sobe as duas APIs .NET, o serviço de previsão em Python e o Node-RED (ponte MQTT do ESP32), com monitoramento de CPU, memória e disco, logs de auditoria e serviços systemd que voltam sozinhos depois de uma falha.

**Vídeo:** https://youtu.be/ULz_PL6kzJI ([`videos/os-provisionamento-vm.mp4`](videos/os-provisionamento-vm.mp4))

## Integrantes

| Nome | RM |
|---|---|
| Leonardo de Farias | 555211 |
| Gustavo Laur | 556603 |
| Giancarlo Cestarolli | 555248 |

## O que tem nesta pasta

| Caminho | Conteúdo |
|---|---|
| `infra/01_rede.sh` | hostname, IP estático ou DHCP (netplan), DNS, fuso horário e firewall `ufw` |
| `infra/02_app.sh` | instala .NET 10 e Python, cria o usuário de serviço, grava as variáveis em `/etc/sampaia/sampaia.env`, publica as APIs, aplica migrações (se houver Oracle) e liga os serviços |
| `infra/03_nodered.sh` | instala Node.js e Node-RED e liga a ponte MQTT entre o ESP32 e a API de Lembretes |
| `infra/monitor.sh` | monitora CPU, memória, disco e a saúde das APIs, com alertas em `/var/log/sampaia/alertas.log` e no journal |
| `infra/comum.sh` | log de cada execução e log de auditoria (quem rodou, quando e o resultado) |
| `infra/teste_falha.sh` | `kill -9` na API do Agente e no monitor, mostrando o systemd subindo os dois de novo |
| `infra/systemd/` | units dos serviços (`Restart=always`) e o timer do monitor |
| `infra/node-red/` | fluxo e configuração do Node-RED |
| `infra/evidencias/` | saídas reais de uma VM limpa: rede, aplicação, alertas, auditoria, serviços, teste de falha e o log completo do provisionamento |
| `backend/`, `ml/`, `security/regras-pii.json` | a aplicação que a VM hospeda: as duas APIs .NET e o serviço de previsão |

## Como a solução atende a disciplina

| Tema | Onde está |
|---|---|
| Provisionamento de infra (rede, DNS, IP) | `infra/01_rede.sh` |
| Provisionamento da aplicação (dependências, configuração, orquestração) | `infra/02_app.sh` e `infra/03_nodered.sh` |
| Monitoramento com alertas de CPU, disco e memória | `infra/monitor.sh` → `infra/evidencias/alertas.txt` |
| Monitoramento como serviço tolerante a falhas | `infra/systemd/sampaia-monitor.service` (`Restart=always`) + `sampaia-monitor.timer` |
| Logs de auditoria e de execução | `infra/comum.sh` → `infra/evidencias/auditoria.txt` |
| Funcionamento completo | `infra/teste_falha.sh` → `infra/evidencias/teste_falha.txt`; provisionamento completo em `infra/evidencias/provisionamento-vm.txt` |

Todos os scripts usam `set -euo pipefail`, são idempotentes (rodar duas vezes não quebra nada) e passam no `shellcheck`.

## Como rodar (dentro da VM, a partir desta pasta)

```bash
sudo IP=192.168.56.10/24 GATEWAY=192.168.56.1 ./infra/01_rede.sh      # sem IP = DHCP
sudo ./infra/02_app.sh                     # opcional: ANTHROPIC_API_KEY=... ORACLE_CONN='...'
curl http://localhost:5009/api/servicos    # API respondendo
sudo ./infra/03_nodered.sh                 # ponte MQTT do ESP32
sudo ./infra/teste_falha.sh                # kill -9 e systemd reiniciando
sudo tail /var/log/sampaia/auditoria.log /var/log/sampaia/alertas.log
```

Sem `ORACLE_CONN` as APIs usam SQLite; sem `ANTHROPIC_API_KEY`, o LLM simulado. Roteiro completo e tabela de serviços em [`infra/README.md`](infra/README.md).

## Ficha da CitizenTech

| Campo | Resposta |
|---|---|
| Nome da CitizenTech | SampaIA (a assistente se chama **Paulinha**) |
| Cidade e bairro | São Paulo (SP), a cidade toda: os serviços (Sabesp, IPTU, IPVA, SP156, Poupatempo) são os mesmos em qualquer bairro |
| Cidadão beneficiado | Idosos e pessoas com pouca familiaridade digital em São Paulo, que hoje dependem de um parente ou vizinho para pagar uma conta ou marcar um atendimento |
| Problema, em uma frase | Em São Paulo o serviço público já é digital, mas quem tem pouca familiaridade com a internet fica preso em sites confusos e depende de outra pessoa para resolver coisas simples |
| Como funciona | A pessoa escreve o que precisa ("quero pagar meu IPTU"). A Paulinha, uma extensão do Chrome, monta o plano, navega no site oficial e preenche o que dá. No login gov.br ela pausa e mostra um tutorial; a pessoa entra sozinha e o agente retoma. Nada irreversível acontece sem "Confirmo". Dados pessoais são mascarados antes de ir para a IA. O app avisa vencimentos, o ESP32 dá o lembrete físico em casa e o modelo de ML estima o prazo dos pedidos do SP156 |
| O que cada parte faz | API do Agente e API de Lembretes em C#/.NET (SOA e C#), regras de dados pessoais e Risco Cidadão (Cybersecurity), previsão de prazo do SP156 (IA & ML), app Expo (Mobile), VM Linux provisionada por Bash (Operating Systems), lembrete físico com ESP32 (Physical Computing) e plano de testes no Azure Boards (Testing) |
| Ecossistema | **Cidadão:** faz o pedido, entra sozinho na conta e dá o "Confirmo". **Família ou quem ajuda:** instala a extensão e o app uma vez. **Órgãos públicos e concessionárias** (Prefeitura, Sabesp, Secretaria da Fazenda, Poupatempo, gov.br): a Paulinha usa os sites públicos como estão. **Anthropic (Claude):** decide o próximo passo e só recebe texto já mascarado. **Dados abertos da Prefeitura:** base do modelo de prazo do SP156 |
| Indicador de impacto | Tarefas concluídas sem ajuda presencial (%); tempo médio por serviço comparado a fazer sozinho; logins retomados após o tutorial (%); Risco Cidadão médio por tarefa, que deve ficar baixo |
| ODS conectados | **10 — Redução das desigualdades** (meta 10.2): a pessoa idosa resolve sozinha o que hoje depende de outra pessoa. **11 — Cidades e comunidades sustentáveis** (meta 11.3): o app ajuda a abrir e acompanhar pedidos de zeladoria no SP156 e mostra quanto tempo a Prefeitura costuma levar. **9 — Indústria, inovação e infraestrutura** (meta 9.c): IA aplicada para tornar acessíveis os serviços digitais que já existem |

---
Global Solution CitizenTech · FIAP · Engenharia de Software, 3º ano · 2º semestre de 2026. Todos os dados de teste e de demonstração são fictícios; o modelo de ML usa dados públicos e anonimizados do SP156.
