# SampaIA — Inteligência Artificial & Machine Learning

Pergunta de ML: **em quanto tempo um pedido de zeladoria do SP156 é atendido?** O modelo classifica o pedido em três faixas (até 7 dias, 8 a 30 dias, mais de 30 dias) a partir do tipo do problema, do distrito e da data. O app usa essa previsão para dizer ao cidadão quanto tempo a Prefeitura costuma levar.

**Vídeo:** https://youtu.be/LKwL2Yb8uCs ([`videos/ia-ml-prazo-sp156.mp4`](videos/ia-ml-prazo-sp156.mp4))

## Integrantes

| Nome | RM |
|---|---|
| Leonardo de Farias | 555211 |
| Gustavo Laur | 556603 |
| Giancarlo Cestarolli | 555248 |

## O que tem nesta pasta

| Caminho | Conteúdo |
|---|---|
| `ml/notebook.ipynb` | notebook executado: dados, exploração, Regressão Logística (base), MLP em PyTorch com 12 combinações de hiperparâmetros e validação cruzada, métricas de treino × teste, curva de erro e conclusão |
| `ml/RELATORIO_ML.md` | relatório: problema, dados, escolha do modelo, preparação, treino, resultados e leitura para o cidadão |
| `ml/dados/README.md` | fonte, licença, período, colunas e recorte dos dados |
| `ml/requirements.txt` | dependências Python |
| `ml/preparar.py` | limpeza, codificação e a rede `RedePrazo` (usados pelo notebook e pelo serviço) |
| `ml/baixar_dados.py` | baixa os CSVs reais pela API do Portal de Dados Abertos (não vão no repositório, ~415 MB) |
| `ml/servico.py` | serviço FastAPI `POST /prever-prazo` (porta 8010) |
| `ml/modelo.joblib` | modelo treinado (pesos, colunas e padronização) |
| `ml/test_servico.py` | testes do serviço (`pytest`) |
| `ml/gerar_distritos.py` | gera o centro de cada distrito, usado pelo app para achar o bairro sem enviar a localização |
| `ml/prints/` | gráficos: exploração, matriz de confusão, curva de aprendizado e o `/docs` do serviço |

## Dados

Dados reais e públicos: [Dados do SP156](https://dados.prefeitura.sp.gov.br/dataset/dados-do-sp156), Portal de Dados Abertos da Prefeitura de São Paulo (SMIT), licença CC0. Quatro trimestres (julho/2025 a junho/2026): 2.023.061 pedidos, dos quais **373.838 pedidos de zeladoria finalizados** entram no modelo (entulho, buraco, árvore, varrição, bueiro). O alvo é `Data de Finalização − Data de Abertura`, em dias.

## Resultado

| Modelo | Acurácia treino | Acurácia teste | Recall macro treino | Recall macro teste |
|---|---|---|---|---|
| Regressão Logística (base) | 0,676 | 0,674 | 0,659 | 0,657 |
| **MLP (64 neurônios, taxa 0,01, 15 épocas)** | 0,685 | **0,681** | 0,710 | **0,705** |

A diferença pequena entre treino e teste indica que o modelo não decorou os dados. A melhor combinação foi escolhida por validação cruzada só no treino (divisão 80/20 estratificada: 299.070 pedidos no treino, 74.768 no teste).

## Como rodar

```bash
cd ml
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt
.venv/bin/uvicorn servico:app --port 8010   # usa o modelo.joblib já treinado; documentação em /docs
.venv/bin/pytest
```

Para treinar de novo, veja [`ml/README.md`](ml/README.md) (Google Colab ou local).

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
