"""Serviço de previsão de prazo do SP156.

Rodar: uvicorn servico:app --port 8010
Chamado pela API do Agente (POST /api/previsao-prazo), que repassa o que o app enviou.
"""
import unicodedata
from datetime import date
from pathlib import Path

import joblib
import pandas as pd
import torch
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from preparar import CATEGORIAS, FAIXAS_TEXTO, ROTULOS_APP, RedePrazo, codificar, linha_para_prever

ARQUIVO_MODELO = Path(__file__).parent / "modelo.joblib"

def normalizar_distrito(nome: str | None) -> str:
    """"Butantã" -> "BUTANTA": os dados do SP156 (e o modelo) guardam os distritos sem acento."""
    sem_acento = unicodedata.normalize("NFKD", nome or "").encode("ascii", "ignore").decode()
    return " ".join(sem_acento.upper().split())


app = FastAPI(title="SampaIA — previsão de prazo do SP156", version="1.0")
_pacote: dict | None = None


def pacote() -> dict:
    global _pacote
    if _pacote is None:
        if not ARQUIVO_MODELO.exists():
            raise HTTPException(503, "Modelo ainda não treinado. Execute o notebook.ipynb.")
        pk = joblib.load(ARQUIVO_MODELO)
        rede = RedePrazo(len(pk["colunas"]), pk["oculta"])
        rede.load_state_dict(pk["pesos"])
        pk["rede"] = rede.eval()
        _pacote = pk
    return _pacote


def probabilidades(pk: dict, entrada: pd.DataFrame):
    """Mesmo caminho do notebook: colunas 0/1 do treino, padronização do treino, softmax nas 3 faixas."""
    x = (codificar(entrada, pk["colunas"]).to_numpy() - pk["media"]) / pk["escala"]
    with torch.no_grad():
        return torch.softmax(pk["rede"](torch.tensor(x, dtype=torch.float32)), dim=1).numpy()


class Pedido(BaseModel):
    tipoServico: str  # rótulo do app ("Buraco na rua") ou categoria ("buraco")
    distrito: str | None = None  # só o bairro/distrito; nada de endereço
    dataAbertura: date | None = None


class Previsao(BaseModel):
    faixa: str
    classe: str
    fonte: str
    probabilidades: dict[str, float]


@app.post("/prever-prazo", response_model=Previsao)
def prever(p: Pedido) -> Previsao:
    categoria = ROTULOS_APP.get(p.tipoServico, p.tipoServico)
    if categoria not in CATEGORIAS:
        raise HTTPException(400, f"Tipo de pedido desconhecido. Use um destes: {', '.join(ROTULOS_APP)}")

    pk = pacote()
    data = pd.Timestamp(p.dataAbertura or date.today())
    distrito = normalizar_distrito(p.distrito)
    if distrito in pk["mapa_subprefeitura"]:
        entrada = linha_para_prever(categoria, distrito, data, pk["mapa_subprefeitura"])
    else:
        # Bairro desconhecido: média de todos os distritos. "NÃO INFORMADO" nos dados reais fecha rápido
        # (pedido sem endereço costuma ser cancelado ou redirecionado) e daria uma previsão otimista demais.
        entrada = pd.concat([linha_para_prever(categoria, d, data, pk["mapa_subprefeitura"]) for d in pk["mapa_subprefeitura"] if d != "NÃO INFORMADO"])
    probs = probabilidades(pk, entrada).mean(axis=0)
    classe = pk["faixas"][int(probs.argmax())]
    return Previsao(
        faixa=FAIXAS_TEXTO[classe],
        classe=classe,
        fonte="modelo",
        probabilidades={FAIXAS_TEXTO[f]: round(float(x), 3) for f, x in zip(pk["faixas"], probs)},
    )


@app.get("/saude")
def saude() -> dict:
    pk = pacote()
    return {"status": "ok", "periodo": pk["periodo"], "metricas_teste": pk["metricas_teste"]}
