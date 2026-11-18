# SampaIA

> "Oi, Paulinha, quero pagar meu IPTU."

A SampaIA é um agente de IA que resolve serviços públicos de São Paulo **junto com o cidadão**, pensado para idosos e pessoas com pouca familiaridade digital. A assistente, a **Paulinha**, é uma extensão do Chrome: navega nos sites oficiais (Sabesp, IPTU, IPVA, SP156, Poupatempo), preenche o que dá, pausa no login gov.br para a pessoa entrar sozinha e só faz algo definitivo depois de um "Confirmo". Dados pessoais são mascarados antes de qualquer envio à IA.

## Integrantes

| Nome | RM |
|---|---|
| Leonardo de Farias | 555211 |
| Gustavo Laur | 556603 |
| Giancarlo Cestarolli | 555248 |

## Entregas por disciplina

Cada pasta traz a parte da SampaIA de uma disciplina, com o seu próprio README.

| Disciplina | Pasta | O que tem | Vídeo |
|---|---|---|---|
| Arquitetura Orientada a Serviços (SOA) e Web Services | [`1-soa/`](1-soa/) | API do Agente e API de Lembretes, banco, migrações, Scalar e testes | — |
| Cybersecurity | [`2-cybersecurity/`](2-cybersecurity/) | regras de PII, taint analysis, eventos e Risco Cidadão | https://youtu.be/O8g4YKnLzHQ |
| C# Software Development | [`3-csharp/`](3-csharp/) | backend .NET 10: POO, Web API, EF Core + LINQ, async e testes | — |
| Inteligência Artificial & Machine Learning | [`4-ia-ml/`](4-ia-ml/) | previsão de prazo do SP156 (MLP em PyTorch), notebook e relatório | https://youtu.be/LKwL2Yb8uCs |
| Mobile Development & IOT | [`5-mobile/`](5-mobile/) | app Expo com vencimentos, documentos, pedido ao 156 e Segurança | https://youtu.be/q22QP1ius8Y |
| Operating Systems | [`6-operating-systems/`](6-operating-systems/) | VM Linux provisionada por Bash, systemd, monitoramento e teste de falha | https://youtu.be/ULz_PL6kzJI |
| Physical Computing: IOT & IOB | [`7-physical-computing/`](7-physical-computing/) | lembrete físico com ESP32 no [Wokwi](https://wokwi.com/projects/480882620502191105) | https://youtu.be/aM7IXJtgf8U |
| Testing, Compliance & Quality Assurance | [`8-testing/`](8-testing/) | Features e Test Cases no [Azure Boards](https://dev.azure.com/RM555211/SampaIA) e o pitch | https://youtu.be/_W-0qI-vqD4 |

## Como as partes se conectam

```
Sites oficiais (Sabesp, IPTU, IPVA, SP156, Poupatempo)
        ↕ lê e age
Extensão Chrome (Paulinha)     App Expo (5)     ESP32 (7)
        ↓                          ↓               ↓ MQTT + Node-RED
   API do Agente (1, 3) ──────→ API de Lembretes (1, 3)
     │        │       └──→ Previsão de prazo do SP156 (4)
     ↓        └──→ LLM (Claude), só com texto mascarado (2)
   Oracle (EF Core) [SQLite em desenvolvimento]
Servidor: VM Linux provisionada por scripts Bash (6) · Testes de aceite no Azure Boards (8)
```

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
