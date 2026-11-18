"""Gera mobile/src/lib/distritos.ts: o ponto central de cada distrito de São Paulo.

Fonte: CSVs do SP156 (Portal de Dados Abertos da Prefeitura de SP, CC0), baixados com baixar_dados.py.
Para cada distrito, usa a MEDIANA da latitude e da longitude de todos os pedidos daquele distrito
(milhares de pontos agregados: nenhum endereço individual vai para o app).

O app usa a tabela para achar o distrito mais próximo da pessoa sem mandar a posição para lugar nenhum,
e com os mesmos nomes que o modelo de ML conhece.

Uso: .venv/bin/python gerar_distritos.py
"""
from pathlib import Path

import pandas as pd

PASTA = Path(__file__).parent / "dados"
SAIDA = Path(__file__).parent.parent / "mobile" / "src" / "lib" / "distritos.ts"
MINIMO_DE_PEDIDOS = 50


def main() -> None:
    arquivos = sorted(PASTA.glob("*.csv"))
    if not arquivos:
        raise SystemExit("Nenhum CSV em ml/dados. Rode antes: .venv/bin/python baixar_dados.py")
    partes = [
        pd.read_csv(a, sep=";", encoding="latin-1", usecols=["Latitude", "Longitude", "Distrito"], dtype=str)
        for a in arquivos
    ]
    df = pd.concat(partes).dropna()
    df["lat"] = pd.to_numeric(df["Latitude"].str.replace(",", "."), errors="coerce")
    df["lon"] = pd.to_numeric(df["Longitude"].str.replace(",", "."), errors="coerce")
    df["distrito"] = df["Distrito"].str.strip().str.upper()
    # Só pontos dentro da caixa do município (descarta coordenadas zeradas ou erradas).
    df = df[df["lat"].between(-24.01, -23.35) & df["lon"].between(-46.83, -46.36)]
    df = df[df["distrito"] != "NÃO INFORMADO"]

    centros = df.groupby("distrito").agg(lat=("lat", "median"), lon=("lon", "median"), n=("lat", "size"))
    centros = centros[centros["n"] >= MINIMO_DE_PEDIDOS].sort_index()

    linhas = [f"  ['{d}', {r.lat:.4f}, {r.lon:.4f}]," for d, r in centros.iterrows()]
    SAIDA.write_text(
        "// GERADO por ml/gerar_distritos.py a partir dos dados abertos do SP156 (CC0). Não edite à mão.\n"
        f"// {len(centros)} distritos: [nome igual ao do modelo de ML, latitude, longitude] = mediana dos pedidos do distrito.\n"
        "export const CENTROS_DOS_DISTRITOS: readonly (readonly [string, number, number])[] = [\n"
        + "\n".join(linhas)
        + "\n];\n",
        encoding="utf-8",
    )
    print(f"{len(centros)} distritos -> {SAIDA}")
    print(centros.sort_values("n").head(5))


if __name__ == "__main__":
    main()
