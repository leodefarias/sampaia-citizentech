# SampaIA — Physical Computing: IOT & IOB

Lembrete físico para quem não usa bem o celular: um **ESP32** na geladeira ou na sala mostra o próximo vencimento ("CONTA DE AGUA / vence em 2 dias"). Na véspera, acende um LED e toca um buzzer. O botão **"Já paguei"** marca a conta como paga na mesma API de Lembretes que o app e a Paulinha usam.

**Projeto no Wokwi:** https://wokwi.com/projects/480882620502191105
**Vídeo:** https://youtu.be/aM7IXJtgf8U ([`videos/iot-esp32-wokwi.mp4`](videos/iot-esp32-wokwi.mp4))

## Integrantes

| Nome | RM |
|---|---|
| Leonardo de Farias | 555211 |
| Gustavo Laur | 556603 |
| Giancarlo Cestarolli | 555248 |

## O que tem nesta pasta

| Caminho | Conteúdo |
|---|---|
| `iot/sketch.ino` | firmware do ESP32 (Wi-Fi, MQTT, display, LED, buzzer e botão) |
| `iot/diagram.json` | circuito do Wokwi |
| `iot/libraries.txt` | bibliotecas: Adafruit SSD1306, Adafruit GFX Library, ArduinoJson, PubSubClient |
| `iot/integrantes.txt` | nomes e RMs dos integrantes |
| `iot/README.md` | funcionamento detalhado e como montar no Wokwi |

## Circuito

| Peça | Pino |
|---|---|
| Display OLED SSD1306 (I2C) | SDA 21, SCL 22 |
| LED vermelho + resistor 220 Ω | 2 |
| Buzzer | 15 |
| Botão "Já paguei" | 4 (INPUT_PULLUP) |

## Como o circuito se integra à solução

1. O ESP32 se conecta ao Wi-Fi do Wokwi e a um broker MQTT público e recebe o próximo lembrete no tópico `sampaia/<dispositivo>/lembrete`.
2. Quem consulta a API de Lembretes é o Node-RED da VM da SampaIA (pasta `6-operating-systems`), ao iniciar, a cada 30 minutos e quando o ESP32 pede.
3. Na véspera do vencimento, o LED acende e o buzzer toca três vezes.
4. O botão "Já paguei" publica o pedido de conclusão; o Node-RED marca a conta como paga na API e devolve o próximo lembrete.
5. Sem resposta em 15 segundos (VM ou broker fora do ar), o ESP32 mostra um lembrete de exemplo e continua funcionando.

Para ver funcionando, abra o [projeto no Wokwi](https://wokwi.com/projects/480882620502191105) e inicie a simulação.

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
