// Rodar: npm test (Node 22+ executa TypeScript direto).
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { diaFalado, quandoVence } from './datas.ts';

const quarta = new Date(2026, 10, 4); // 04/11/2026

test('linguagem simples para vencimentos', () => {
  assert.equal(quandoVence('2026-11-04', quarta), 'vence hoje');
  assert.equal(quandoVence('2026-11-05', quarta), 'vence amanhã');
  assert.equal(quandoVence('2026-11-06', quarta), 'vence sexta');
  assert.equal(quandoVence('2026-11-24', quarta), 'vence dia 24/11');
  assert.equal(quandoVence('2026-11-02', quarta), 'venceu há 2 dias');
  assert.equal(diaFalado('2026-11-11', quarta), 'dia 11/11');
});
