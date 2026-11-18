// Rodar: npm test (Node 22+ executa TypeScript direto).
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { distritoMaisProximo, nomeDoDistrito } from './localizacao.ts';

test('acha o distrito pelos pontos conhecidos da cidade', () => {
  assert.equal(distritoMaisProximo(-23.5503, -46.6339), 'SE'); // Praça da Sé
  assert.equal(distritoMaisProximo(-23.9000, -46.7200), 'MARSILAC'); // extremo sul, área rural
  // Perto da divisa entre distritos o resultado é aproximado; por isso a tela diz "Perto de".
});

test('fora da capital não inventa distrito', () => {
  assert.equal(distritoMaisProximo(-22.9068, -43.1729), null); // Rio de Janeiro
  assert.equal(distritoMaisProximo(-22.9056, -47.0608), null); // Campinas
});

test('nome amigável para a tela', () => {
  assert.equal(nomeDoDistrito('VILA MARIANA'), 'Vila Mariana');
  assert.equal(nomeDoDistrito('FREGUESIA DO O'), 'Freguesia do Ó');
  assert.equal(nomeDoDistrito('SE'), 'Sé');
  assert.equal(nomeDoDistrito('JARDIM SAO LUIS'), 'Jardim São Luís');
});
