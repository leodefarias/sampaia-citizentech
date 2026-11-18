# SampaIA — Testing, Compliance & Quality Assurance

Planejamento e qualificação da SampaIA no **Azure Boards**: 9 Features escritas como histórias de usuário, com critérios de aceite, e 26 Test Cases funcionais caixa-preta, positivos e negativos, para a homologação da solução.

**Projeto no Azure Boards:** https://dev.azure.com/RM555211/SampaIA
**Pitch (até 4 min):** https://youtu.be/_W-0qI-vqD4 ([`videos/pitch.mp4`](videos/pitch.mp4))

## Integrantes

| Nome | RM |
|---|---|
| Leonardo de Farias | 555211 |
| Gustavo Laur | 556603 |
| Giancarlo Cestarolli | 555248 |

## O que tem nesta pasta

| Caminho | Conteúdo |
|---|---|
| `qa/entrega-testing.pdf` | documento com o link do Azure Boards e os prints de todas as Features e Test Cases (lista e detalhes) |
| `qa/features-e-testes.md` | as mesmas Features e Test Cases em texto |
| `videos/pitch.mp4` | vídeo do pitch: problema, solução funcionando e quem ela ajuda |

## Features

Cada Feature tem história de usuário ("Como …, quero …, para …") e critérios de aceite em Dado/Quando/Então. Todas estão em *Closed* (testes de aceite aprovados) e ligadas aos seus Test Cases por *Tested By*.

| Feature | Título | Test Cases | Responsável |
|---|---|---|---|
| F1 | Pedir uma tarefa ao agente | CT-01 a CT-03 | Leonardo |
| F2 | Handoff de login | CT-04 a CT-06 | Leonardo |
| F3 | Confirmação de ação final | CT-07 a CT-09 | Leonardo |
| F4 | 2ª via Sabesp | CT-10 e CT-11 | Gustavo |
| F5 | Guia do IPTU | CT-12 e CT-13 | Gustavo |
| F6 | IPVA com QR code | CT-14 e CT-15 | Gustavo |
| F7 | Pedido SP156 com previsão de prazo | CT-16 a CT-18 | Giancarlo |
| F8 | Lembretes de vencimento | CT-19 a CT-22 | Giancarlo |
| F9 | Mascaramento de dados pessoais | CT-23 a CT-26 | Giancarlo |

## Test Cases

26 casos funcionais caixa-preta (17 positivos e 9 negativos), em estado *Ready*. Cada um tem objetivo, preparação, entrada, saída esperada e passos com o resultado esperado em cada passo. Os negativos cobrem, por exemplo: pedido não reconhecido, extensão tentando ler a página de login, usuário desistindo do login ou recusando a confirmação, confirmação fora de hora (HTTP 409), número SQL inválido, serviço de ML fora do ar, ESP32 sem API e CPF injetado na página.

Todos os dados de teste são fictícios (CPF gerado com dígitos válidos, nome "Maria Teste").

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
