"""Baixa os CSVs de solicitações do SP156 (Portal de Dados Abertos da Prefeitura de SP, licença CC0).

Uso: python baixar_dados.py            (trimestres padrão do recorte)
     python baixar_dados.py "4º TRI 2025" "1º TRI 2026"

Lista os recursos pela API CKAN do portal e baixa um arquivo por vez (sem rajadas).
Os CSVs não vão para o git (são grandes); ficam em ml/dados/.
"""
import json
import sys
import urllib.request
from pathlib import Path

API = "https://dados.prefeitura.sp.gov.br/api/3/action/package_show?id=dados-do-sp156"
RECORTE = ["3º TRI 2025", "4º TRI 2025", "1º TRI 2026", "2º TRI 2026"]
PASTA = Path(__file__).parent / "dados"


def normalizar(nome: str) -> str:
    return nome.replace("°", "º").replace("  ", " ").strip()


def main(trimestres: list[str]) -> None:
    with urllib.request.urlopen(API, timeout=60) as r:
        recursos = json.load(r)["result"]["resources"]
    for tri in trimestres:
        alvo = [x for x in recursos if normalizar(x["name"]).endswith(tri) and x["url"].endswith(".csv")]
        if not alvo:
            print(f"não achei CSV de {tri}")
            continue
        destino = PASTA / Path(alvo[0]["url"]).name
        if destino.exists():
            print(f"{destino.name}: já existe")
            continue
        print(f"baixando {tri} -> {destino.name}")
        urllib.request.urlretrieve(alvo[0]["url"], destino)


if __name__ == "__main__":
    main(sys.argv[1:] or RECORTE)
