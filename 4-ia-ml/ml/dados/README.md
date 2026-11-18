# Dados do SP156

| Item | Valor |
|---|---|
| Fonte | [Dados do SP156 — Solicitações](https://dados.prefeitura.sp.gov.br/dataset/dados-do-sp156), Portal de Dados Abertos da Prefeitura de São Paulo |
| Responsável | Secretaria Municipal de Inovação e Tecnologia (SMIT), atualização trimestral |
| Licença | Creative Commons CCZero (CC0) |
| Arquivos usados | `arquivo-final-3-trim-25.csv`, `arquivo-final-4-trim-25.csv`, `arquivo-final-1-trim-26.csv`, `arquivo-final-2-trim-26.csv` |
| Período | pedidos abertos de julho/2025 a junho/2026 (3º tri 2025 a 2º tri 2026) |
| Baixado em | 04/11/2026 |
| Formato | CSV, separador `;`, encoding `cp1252`, ~2 milhões de linhas no total |

**Como baixar:** `python baixar_dados.py` (usa a API CKAN do portal, um arquivo por vez). Os CSVs não vão para o git.

## Colunas (iguais nos 4 arquivos)

`Data de Abertura`, `Data de Finalização`, `Meio de Comunicação`, `Tema`, `Assunto`, `Serviço`, `Status`, `Órgão`, `Logradouro`, `Número`, `CEP`, `Latitude`, `Longitude`, `Prefeitura Operacional`, `Distrito`

- **Existe data de conclusão** (`Data de Finalização`), então o alvo é o tempo real de atendimento. Ela falta em 8,8% dos pedidos (abertos, em andamento ou cancelados).
- `Status`: Finalizada (90,7%), Aberto, Cancelada, Em Andamento.
- Os dados já vêm sem identificação do cidadão. Mesmo assim, o modelo **não usa** logradouro, número, CEP nem coordenadas.

## Recorte de zeladoria

| Categoria | Serviços do SP156 |
|---|---|
| buraco | Tapa-buraco; Tapa-buraco em faixa exclusiva ou corredor de ônibus |
| entulho | Entulho e grandes objetos em via pública; Descarte irregular de entulho |
| arvore | Avaliação para poda ou remoção; Queda de árvore; Risco iminente de queda |
| bueiro | Limpeza de bueiros, boca de lobo e poços de visita |
| varricao | Reclamação de falta de varrição |

Iluminação pública **não aparece** no SP156 nesse período (é atendida por outro canal), por isso ficou fora.

Resultado: **373.838 pedidos de zeladoria finalizados**. Faixas: até 7 dias 72,8% · 8 a 30 dias 16,2% · mais de 30 dias 11,0%.
