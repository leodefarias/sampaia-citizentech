// Mesmas regras do PiiMasker em C# (security/regras-pii.json). Sanitizador da taint analysis.
import arquivo from '../../security/regras-pii.json';

interface RegraJson {
  nome: string;
  padrao: string;
  flags?: string;
  marcador?: string;
  validador?: string;
  bloqueia?: boolean;
}

export interface ResultadoMascara {
  texto: string;
  bloqueado: boolean;
  encontrados: string[];
}

export const BLOQUEADO = '[BLOQUEADO]';

const regras = (arquivo.regras as RegraJson[]).map((r) => ({
  ...r,
  re: new RegExp(r.padrao, 'g' + (r.flags ?? '')),
}));

const digitos = (s: string) => [...s].filter((c) => c >= '0' && c <= '9').map(Number);

export function cpfValido(d: number[]): boolean {
  if (d.length !== 11 || d.every((x) => x === d[0])) return false;
  for (let n = 9; n <= 10; n++) {
    let soma = 0;
    for (let i = 0; i < n; i++) soma += d[i]! * (n + 1 - i);
    if (((soma * 10) % 11) % 10 !== d[n]) return false;
  }
  return true;
}

export const cnsValido = (d: number[]) =>
  d.length === 15 && d.reduce((s, x, i) => s + x * (15 - i), 0) % 11 === 0;

export function luhnValido(d: number[]): boolean {
  if (d.length < 13 || d.length > 19) return false;
  let soma = 0;
  for (let i = 0; i < d.length; i++) {
    let x = d[d.length - 1 - i]!;
    if (i % 2 === 1) {
      x *= 2;
      if (x > 9) x -= 9;
    }
    soma += x;
  }
  return soma % 10 === 0;
}

const validadores: Record<string, (d: number[]) => boolean> = { cpf: cpfValido, cns: cnsValido, luhn: luhnValido };

export function mascarar(texto: string): ResultadoMascara {
  const encontrados: string[] = [];
  for (const r of regras) {
    r.re.lastIndex = 0;
    if (r.bloqueia) {
      if (r.re.test(texto)) return { texto: BLOQUEADO, bloqueado: true, encontrados: [r.nome] };
      continue;
    }
    texto = texto.replace(r.re, (m) => {
      if (r.validador && !validadores[r.validador]!(digitos(m))) return m;
      encontrados.push(r.nome);
      return r.marcador!;
    });
  }
  return { texto, bloqueado: false, encontrados };
}

/** Atalho para sinks: devolve só o texto seguro. */
export const seguro = (texto: string) => mascarar(texto).texto;
