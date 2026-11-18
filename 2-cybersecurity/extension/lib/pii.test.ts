import { describe, expect, it } from 'vitest';
import { BLOQUEADO, mascarar } from './pii';

// CPF sintético: dígitos verificadores calculados a partir de 123456789.
const CPF = '123.456.789-09';

describe('mascarar (mesmas regras do C#)', () => {
  it.each([
    [`CPF ${CPF}`, 'CPF [CPF]'],
    ['CPF 12345678909', 'CPF [CPF]'],
    ['123.456.789-00', '[DOC]'],
    ['maria.teste@exemplo.com.br', '[EMAIL]'],
    ['(11) 98765-4321', '[TEL]'],
    ['4111 1111 1111 1111', '[PAGAMENTO]'],
    ['CEP 01310-100', 'CEP [ENDERECO]'],
    ['Mora na Rua das Flores, 123', 'Mora na [ENDERECO]'],
    ['Fica na rua da Consolação 200', 'Fica na [ENDERECO]'],
    ['Av. Paulista, 1000', '[ENDERECO]'],
    ['Rua E, 45', '[ENDERECO]'],
    ['Rua E 45', '[ENDERECO]'],
    // Fala da Paulinha (teste no SP156): "rua e número" é instrução, não endereço.
    ['Digite o endereço (nome da rua e número), depois escolha na lista.', 'Digite o endereço (nome da rua e número), depois escolha na lista.'],
    ['Diagnóstico', '[SAUDE]'],
    ['Valor R$ 123,45 vence em 10/12/2026', 'Valor R$ 123,45 vence em 10/12/2026'],
    ['Número SQL 000.000.0000-0', 'Número SQL 000.000.0000-0'],
  ])('%s', (entrada, esperado) => expect(mascarar(entrada).texto).toBe(esperado));

  it('senha bloqueia tudo', () => {
    const r = mascarar('Senha: abc123');
    expect(r.bloqueado).toBe(true);
    expect(r.texto).toBe(BLOQUEADO);
  });

  it('pode ser chamado várias vezes (regex global sem estado)', () => {
    expect(mascarar(`a ${CPF}`).texto).toBe('a [CPF]');
    expect(mascarar(`b ${CPF}`).texto).toBe('b [CPF]');
  });
});
