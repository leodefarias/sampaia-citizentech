"""Preparação dos dados do SP156 para a pergunta: em quanto tempo um pedido de zeladoria é atendido?

Usado pelo notebook (treino) e pelo serviço (previsão), para as duas pontas tratarem os dados igual.
"""
from pathlib import Path

import pandas as pd
import torch.nn as nn
import torch

PASTA_DADOS = Path(__file__).parent / "dados"

# Serviços de zeladoria agrupados nas categorias que o cidadão escolhe no app.
# Nomes copiados dos CSVs (3º tri 2025 a 2º tri 2026). Iluminação pública não aparece no SP156.
CATEGORIAS = {
    "buraco": ["Tapa-buraco", "Tapa-buraco em faixa exclusiva ou corredor de ônibus"],
    "entulho": [
        "Entulho e grandes objetos em via pública - solicitar remoção",
        "Denunciar descarte irregular de entulho e outros resíduos em área pública",
    ],
    "arvore": [
        "Árvore – Solicitar avaliação em calçadas e praças para fins de poda ou remoção",
        "Queda de árvore",
        "Risco iminente de queda de árvore",
    ],
    "bueiro": ["Solicitar limpeza de bueiros, boca de lobo e poços de visita"],
    "varricao": ["Fazer reclamação de falta de varrição"],
}

# Rótulos do app (mobile/src/app/pedido.tsx) -> categoria.
ROTULOS_APP = {
    "Buraco na rua": "buraco",
    "Entulho ou lixo na rua": "entulho",
    "Árvore (poda ou queda)": "arvore",
    "Bueiro entupido": "bueiro",
    "Falta de varrição": "varricao",
}

FAIXAS = ["ate7", "8a30", "mais30"]
FAIXAS_TEXTO = {"ate7": "até 7 dias", "8a30": "8 a 30 dias", "mais30": "mais de 30 dias"}

VARIAVEIS = ["categoria", "subprefeitura", "distrito", "canal", "mes", "dia_semana"]
TEXTOS = ["categoria", "subprefeitura", "distrito", "canal"]

_SERVICO_PARA_CATEGORIA = {s: c for c, servicos in CATEGORIAS.items() for s in servicos}


def carregar(pasta: Path = PASTA_DADOS) -> pd.DataFrame:
    """Lê todos os CSVs baixados (separador ';', encoding cp1252, formato da Prefeitura)."""
    arquivos = sorted(pasta.glob("*.csv*"))  # .csv ou .csv.gz (upload compactado no Colab)
    if not arquivos:
        raise FileNotFoundError(f"Nenhum CSV em {pasta}. Rode: python baixar_dados.py")
    return pd.concat(
        (pd.read_csv(a, sep=";", encoding="cp1252", dtype=str) for a in arquivos), ignore_index=True
    )


def faixa(dias: pd.Series) -> pd.Series:
    return pd.cut(dias, bins=[-1, 7, 30, float("inf")], labels=FAIXAS).astype(str)


def preparar(bruto: pd.DataFrame) -> pd.DataFrame:
    """Filtra zeladoria finalizada, calcula o tempo de atendimento e as variáveis do modelo.

    Não usa endereço, CEP nem coordenadas: só tipo do pedido, região, canal e data.
    """
    df = bruto[bruto["Serviço"].isin(_SERVICO_PARA_CATEGORIA)].copy()
    df["abertura"] = pd.to_datetime(df["Data de Abertura"], format="%d/%m/%Y", errors="coerce")
    df["finalizacao"] = pd.to_datetime(df["Data de Finalização"], format="%d/%m/%Y", errors="coerce")

    # Só pedidos finalizados têm tempo de atendimento conhecido.
    df = df[(df["Status"] == "Finalizada") & df["abertura"].notna() & df["finalizacao"].notna()]
    df["dias"] = (df["finalizacao"] - df["abertura"]).dt.days
    df = df[df["dias"] >= 0]

    saida = pd.DataFrame(
        {
            "categoria": df["Serviço"].map(_SERVICO_PARA_CATEGORIA),
            "subprefeitura": df["Prefeitura Operacional"].fillna("NÃO INFORMADO").str.strip().str.upper(),
            "distrito": df["Distrito"].fillna("NÃO INFORMADO").str.strip().str.upper(),
            "canal": df["Meio de Comunicação"].fillna("NÃO INFORMADO").str.strip().str.upper(),
            "mes": df["abertura"].dt.month,
            "dia_semana": df["abertura"].dt.dayofweek,
            "abertura": df["abertura"],
            "dias": df["dias"],
        }
    )
    saida["faixa"] = faixa(saida["dias"])
    return saida.reset_index(drop=True)


def linha_para_prever(categoria: str, distrito: str | None, data: pd.Timestamp, mapa_subprefeitura: dict[str, str], canal: str = "MOBILE") -> pd.DataFrame:
    """Monta a entrada do modelo a partir do que o app envia (tipo, bairro e data)."""
    distrito = (distrito or "NÃO INFORMADO").strip().upper()
    return pd.DataFrame(
        [
            {
                "categoria": categoria,
                "subprefeitura": mapa_subprefeitura.get(distrito, "NÃO INFORMADO"),
                "distrito": distrito,
                "canal": canal,
                "mes": data.month,
                "dia_semana": data.dayofweek,
            }
        ]
    )


def codificar(df: pd.DataFrame, colunas: list[str] | None = None) -> pd.DataFrame:
    """Texto vira colunas 0/1 (pd.get_dummies). Com `colunas`, alinha às colunas vistas no treino."""
    x = pd.get_dummies(df[VARIAVEIS], columns=TEXTOS, dtype=float)
    return x if colunas is None else x.reindex(columns=colunas, fill_value=0.0)


class RedePrazo(nn.Module):
    """MLP: camadas Linear com relu; a última sai sem ativação (logits)."""

    def __init__(self, entradas: int, oculta: int = 64):
        super().__init__()
        self.layer1 = nn.Linear(entradas, oculta)
        self.layer2 = nn.Linear(oculta, oculta)
        self.layer3 = nn.Linear(oculta, len(FAIXAS))
        self.dropout = nn.Dropout(0.2)

    def forward(self, x):
        x = torch.relu(self.layer1(x))
        x = self.dropout(x)
        x = torch.relu(self.layer2(x))
        return self.layer3(x)
