# Features e casos de teste (Azure Boards)

Espelho do projeto [SampaIA no Azure Boards](https://dev.azure.com/RM555211/SampaIA): 9 **Features** (#46–#54, estado *Closed*, história de usuário + critérios de aceite em Dado/Quando/Então na descrição) e 26 **Test Cases** (#55–#80, estado *Ready*, ligados por *Tested By*, tags `F1`…`F9` e `positivo`/`negativo`). Todos os dados de teste são fictícios (CPF `123.456.789-09`, gerado com dígitos válidos; nome "Maria Teste").

**Ambiente padrão (AMB):** APIs do Agente (5009) e de Lembretes (5010) rodando, portal simulado (`node portal-simulado/servidor.mjs`), extensão carregada no Chrome, app no modo demonstração ou ligado às APIs.

---

## F1 — Pedir uma tarefa ao agente

**História de usuário:** Como **pessoa idosa com pouca familiaridade digital**, quero **escrever com minhas palavras o serviço que preciso**, para **que a Paulinha descubra sozinha qual site e quais passos usar**.

**Descrição:** o cidadão escreve o que precisa em texto livre e a Paulinha monta um plano com o serviço certo.
**Critérios de aceite:**
- Intenção em texto livre vira um plano com o serviço certo (água → Sabesp, IPTU, IPVA, 156, Poupatempo).
- Pedido sem serviço reconhecido recebe uma explicação em linguagem simples, com exemplos.
- O plano aparece numerado no painel.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-01 | Reconhecer o IPTU | AMB | "Oi, Paulinha, quero pagar meu IPTU" | 1. Abrir o painel. 2. Digitar o pedido. 3. Enviar | Plano de 5 passos do IPTU; tarefa em "Em andamento" |
| CT-02 | Reconhecer a conta de água | AMB | "preciso da segunda via da conta de água" | Igual ao CT-01 | Plano da Sabesp |
| CT-03 (neg.) | Pedido não reconhecido | AMB | "bom dia" | Igual ao CT-01 | Mensagem "Não entendi qual serviço você quer. Tente: conta de água, IPTU..." e nenhuma tarefa criada |

## F2 — Handoff de login

**História de usuário:** Como **pessoa idosa**, quero **fazer o login do gov.br sozinha, com um tutorial na tela**, para **não entregar minha senha a ninguém, nem ao agente**.

**Descrição:** ao chegar no login gov.br (ou no login da Sabesp), o agente pausa, mostra um tutorial e retoma depois que a pessoa entra sozinha.
**Critérios de aceite:**
- O agente pausa no login e o painel mostra o tutorial "Entre no gov.br".
- A extensão não consegue ler a página de login (bloqueio do navegador).
- Depois do login, o agente retoma sozinho.
- Fechar o tutorial sem logar encerra a tarefa e mostra o passo a passo manual.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-04 | Pausar e retomar | AMB; aba em `localhost:5173` | "Quero agendar no Poupatempo simulado" | 1. Enviar pedido. 2. Esperar o login simulado abrir. 3. Digitar CPF e senha de teste. 4. Entrar | Painel mostra o tutorial no passo 2; após entrar, mensagem "Você entrou! Agora eu continuo daqui" e o agente preenche o agendamento |
| CT-05 (neg.) | **Extensão tenta ler o login e não consegue** | Igual ao CT-04, parado no login | — | Rodar `npm run e2e` (passo 2) ou, no console do service worker, `chrome.tabs.sendMessage(<aba>, {tipo:'retrato'})` | Erro "Could not establish connection. Receiving end does not exist."; nenhum conteúdo do login chega à API |
| CT-06 (neg.) | **Usuário fecha o tutorial sem logar** | Igual ao CT-04, parado no login | — | Clicar em "Desistir" | Tarefa vai para `falhou` (GET `/api/tarefas/{id}`); painel mostra "Passo a passo para fazer sozinho" |

## F3 — Confirmação de ação final

**História de usuário:** Como **pessoa idosa**, quero **que nada definitivo aconteça sem eu tocar em "Confirmo"**, para **ter certeza de que nenhum boleto, agendamento ou pedido é feito sem minha vontade**.

**Descrição:** nenhuma ação irreversível (gerar boleto, confirmar agendamento, enviar pedido) acontece sem a pessoa tocar em "Confirmo".
**Critérios de aceite:**
- Antes da ação final o painel mostra "Posso continuar?" com "Confirmo" e "Não".
- "Confirmo" executa a ação e gera o documento.
- "Não" encerra a tarefa sem gerar nada.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-07 | Confirmar o agendamento | CT-04 até o resumo | — | Clicar em "Confirmo" | Portal mostra "Agendamento confirmado! Protocolo PT-2026-..."; documento do tipo agendamento em `/api/documentos` |
| CT-08 (neg.) | **Usuário recusa a confirmação** | CT-04 até o resumo | — | Clicar em "Não" | Mensagem "Tudo bem, parei aqui e nada foi gerado"; tarefa `falhou`; nenhum documento novo |
| CT-09 (neg.) | Confirmar fora de hora | Tarefa em andamento | `POST /api/tarefas/{id}/confirmar {"confirmado":true}` | Chamar a API | HTTP 409 "A tarefa não pode ir de Navegando para Navegando" |

## F4 — 2ª via Sabesp

**História de usuário:** Como **morador de São Paulo com pouca familiaridade digital**, quero **tirar a 2ª via da conta de água da Sabesp com ajuda da Paulinha**, para **pagar em dia sem depender de um parente**.

**Descrição:** o agente abre a Agência Virtual, pausa no login da Sabesp e gera a 2ª via da conta em aberto.
**Critérios de aceite:**
- O login da Sabesp (`/auth/login`) é tratado como handoff: o agente não preenche CPF nem senha.
- O valor e o vencimento da conta vão para Documentos e viram lembrete.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-10 | Pausar no login da Sabesp | AMB + `ANTHROPIC_API_KEY` | "2ª via da conta de água" | 1. Enviar. 2. Observar a aba | Agente abre a Agência Virtual e pausa em `novaagenciavirtual.sabesp.com.br/auth/login` com o tutorial |
| CT-11 | Documento e lembrete | Conta de teste da equipe logada | — | 1. Entrar. 2. Confirmar a 2ª via | Boleto em `/api/documentos` com valor e vencimento; lembrete em `/api/lembretes` com aviso na véspera |

## F5 — Guia do IPTU

**História de usuário:** Como **proprietário de imóvel idoso**, quero **emitir a guia do IPTU informando só o número SQL do carnê**, para **pagar o imposto sem me perder no site da Prefeitura**.

**Descrição:** o agente emite a guia do mês a partir do número SQL do imóvel.
**Critérios de aceite:**
- O agente pede o SQL no painel; o número fica só no computador (o LLM vê `[SQL]`).
- SQL inválido é explicado em linguagem simples.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-12 | Pedir o SQL | AMB + chave do LLM | "pagar meu IPTU" | 1. Enviar. 2. Quando pedir o SQL, digitar o número do carnê de teste | Painel pergunta "Preciso de o número SQL do seu imóvel"; histórico mostra "(informado)", nunca o número |
| CT-13 (neg.) | **SQL inválido explicado em linguagem simples** | Igual ao CT-12 | SQL `12345` | Igual ao CT-12 | Painel: "Esse número não parece certo. O número SQL tem 11 números, assim: 000.000.0000-0. Ele aparece no carnê do IPTU..."; nada é preenchido na página. Se o site recusar um SQL com formato certo, o agente explica o erro do site e espera o usuário |

## F6 — IPVA com QR code

**História de usuário:** Como **dono de veículo com pouca familiaridade digital**, quero **consultar os débitos do IPVA e receber o QR code Pix**, para **pagar pelo celular sem enfrentar o site da Fazenda sozinho**.

**Descrição:** consulta dos débitos do veículo no SIVEI (login gov.br), com o captcha resolvido pela pessoa e o QR code Pix exibido.
**Critérios de aceite:**
- O login é gov.br (handoff).
- Captcha sempre vira "Pronto, já fiz" no painel; o agente nunca tenta resolver.
- QR code exibido só depois de "Confirmo".

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-14 | Captcha com o usuário | AMB + chave do LLM | "quanto é o IPVA do meu carro" | 1. Enviar. 2. Entrar no gov.br. 3. Informar o Renavam de teste. 4. No captcha, resolver e tocar "Pronto, já fiz" | Painel mostra o botão "Pronto, já fiz" no captcha; agente continua só depois do toque |
| CT-15 | QR code após confirmação | CT-14 concluído | — | Tocar "Confirmo" | QR code Pix na página; documento do tipo Pix em `/api/documentos` |

## F7 — Pedido SP156 com previsão de prazo

**História de usuário:** Como **morador que quer pedir um conserto no bairro**, quero **abrir o pedido do SP156 pelo app e ver em quanto tempo pedidos como o meu costumam ser atendidos**, para **saber o que esperar e acompanhar a Prefeitura**.

**Descrição:** no app, o cidadão escolhe o tipo do problema, tira foto, usa a localização e vê em quanto tempo pedidos como o dele costumam ser atendidos.
**Critérios de aceite:**
- A previsão vem do modelo de ML treinado com dados reais do SP156 (`fonte: modelo`).
- Sem o serviço de ML, a API responde com estimativa (`fonte: estimativa`) e o app avisa.
- Só tipo e bairro saem do celular.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-16 | Ver previsão | APIs + `uvicorn servico:app` | Tipo "Árvore (poda ou queda)", bairro atual | 1. Abrir "Pedir conserto no bairro". 2. Escolher o tipo. 3. "Usar onde estou agora". 4. "Quanto tempo vai demorar?" | Cartão "Pedidos como o seu costumam levar ..." e "Calculado com dados públicos do SP156" |
| CT-17 (neg.) | Serviço de ML fora do ar | Parar o uvicorn | Igual ao CT-16 | Igual ao CT-16 | Previsão com "Estimativa simples (modelo indisponível agora)"; app não quebra |
| CT-18 | Protocolo salvo | Pedido enviado pela Paulinha | — | Abrir Documentos no app | Protocolo com "Copiar número" |

## F8 — Lembretes de vencimento

**História de usuário:** Como **pessoa idosa que esquece datas de pagamento**, quero **ser avisada no celular e por um aparelho em casa antes do vencimento**, para **não pagar multa nem ter o serviço cortado**.

**Descrição:** vencimentos aparecem no app (com notificação local) e no ESP32 antes do dia.
**Critérios de aceite:**
- O app mostra "vence sexta" / "vence amanhã", com botão "Ouvir".
- O ESP32 acende o LED e toca o buzzer na véspera; "Já paguei" conclui o lembrete.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-19 | Cartão no app | Lembrete vencendo em 2 dias | — | Abrir o app | Cartão destacado "Conta de água vence <dia da semana>" |
| CT-20 | Aviso no ESP32 | Wokwi com a URL da API; lembrete vencendo amanhã | — | Iniciar a simulação | Display "CONTA DE AGUA / vence AMANHA", LED aceso, 3 bipes |
| CT-21 | "Já paguei" | CT-20 | Botão | Apertar o botão | `POST /concluir` 200 no monitor serial; lembrete some de `GET /api/lembretes` |
| CT-22 (neg.) | API fora do ar | Wokwi sem URL válida | — | Iniciar a simulação | Display mostra o exemplo embutido com "(exemplo, sem API)" |

## F9 — Mascaramento de dados pessoais

**História de usuário:** Como **cidadão que usa a Paulinha**, quero **que meus dados pessoais nunca cheguem à IA, aos logs ou ao banco**, para **usar o agente sem medo de vazamento ou golpe**.

**Descrição:** nenhum CPF, documento, contato, endereço, dado de saúde ou de pagamento chega ao LLM, aos logs ou ao armazenamento.
**Critérios de aceite:**
- Página com CPF: o LLM recebe `[CPF]` e é gerado evento de auditoria.
- CPF digitado no chat é gravado como `[CPF]`.
- Logs da API não contêm o CPF.
- A tela Segurança do app mostra o evento em linguagem simples e o Risco Cidadão.

| ID | Objetivo | Preparação | Inputs | Passos | Resultado esperado |
|---|---|---|---|---|---|
| CT-23 (neg.) | **CPF injetado chega ao LLM como `[CPF]`** | AMB; aba em `localhost:5173/teste-pii.html` | "quero agendar no poupatempo, meu CPF é 123.456.789-09" | Rodar `npm run e2e:taint` | Retrato enviado contém `[CPF]` e não o número; evento `PiiBloqueadaNaExtensao` com "Escondemos seu CPF..."; "RESULTADO: OK" |
| CT-24 | Log sem CPF | AMB | Igual ao CT-23 | `grep -c 123.456.789-09 <log da API>` | 0 ocorrências |
| CT-25 | Senha bloqueada | AMB | Texto "Senha: abc123" no chat | Enviar o pedido | Mensagem "Não escreva senhas aqui"; evento `SenhaNoChat` nível alto |
| CT-26 | Evento no app | Após CT-23 | — | Abrir Segurança no app | Frase simples do evento e o Risco Cidadão |

---

### Testes negativos obrigatórios (resumo)

| Pedido do briefing | Caso |
|---|---|
| A extensão tenta ler a página de login gov.br e não consegue | CT-05 |
| O usuário fecha o tutorial sem logar: a tarefa vai para `Falhou` e mostra o passo a passo manual | CT-06 |
| O usuário recusa a confirmação: nada é gerado | CT-08 |
| Um CPF é injetado na página: chega ao LLM como `[CPF]` e gera evento de auditoria | CT-23 |
| SQL inválido: o agente explica o erro em linguagem simples | CT-13 |
