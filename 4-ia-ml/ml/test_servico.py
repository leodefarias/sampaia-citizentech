"""Rodar: pytest  (precisa do modelo.joblib gerado pelo notebook)."""
import pytest
from fastapi.testclient import TestClient

from servico import ARQUIVO_MODELO, app, normalizar_distrito, pacote

pytestmark = pytest.mark.skipif(not ARQUIVO_MODELO.exists(), reason="modelo.joblib não gerado")
cliente = TestClient(app)


def test_previsao_com_rotulo_do_app():
    r = cliente.post("/prever-prazo", json={"tipoServico": "Buraco na rua", "distrito": "Sé", "dataAbertura": "2026-11-04"})
    assert r.status_code == 200
    corpo = r.json()
    assert corpo["faixa"] in ("até 7 dias", "8 a 30 dias", "mais de 30 dias")
    assert abs(sum(corpo["probabilidades"].values()) - 1) < 0.01


def test_distrito_desconhecido_nao_quebra():
    assert cliente.post("/prever-prazo", json={"tipoServico": "bueiro", "distrito": "Lugar Nenhum"}).status_code == 200


def test_tipo_invalido_explica():
    r = cliente.post("/prever-prazo", json={"tipoServico": "lâmpada"})
    assert r.status_code == 400
    assert "Buraco na rua" in r.json()["detail"]


def test_sem_bairro_nao_fica_otimista():
    # Árvore é o pedido mais demorado; sem bairro a previsão não pode cair em "até 7 dias".
    r = cliente.post("/prever-prazo", json={"tipoServico": "Árvore (poda ou queda)"})
    assert r.json()["faixa"] != "até 7 dias"


def test_distrito_com_acento_e_reconhecido():
    # O celular e as pessoas escrevem "Sé" e "Butantã"; o modelo conhece "SE" e "BUTANTA".
    for nome, esperado in [("Sé", "SE"), ("Butantã", "BUTANTA"), ("  freguesia do ó ", "FREGUESIA DO O")]:
        assert normalizar_distrito(nome) == esperado
        assert esperado in pacote()["mapa_subprefeitura"]
