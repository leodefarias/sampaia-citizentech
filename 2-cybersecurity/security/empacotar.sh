#!/usr/bin/env bash
# Monta o ZIP da entrega de Cybersecurity a partir de entregas/2-cybersecurity/
# (regras de PII, código do módulo, testes, evidências e vídeo).
# Uso: bash security/empacotar.sh  →  security/entrega-cybersecurity.zip
set -euo pipefail
cd "$(dirname "$0")/.."
bash entregas/montar.sh >/dev/null
saida="$PWD/security/entrega-cybersecurity.zip"
rm -f "$saida"
(cd entregas/2-cybersecurity && zip -r -q "$saida" .)
echo "Gerado: security/entrega-cybersecurity.zip"
