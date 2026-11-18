// Linguagem simples: "vence sexta", nunca "vencimento 2026-11-06".
const DIAS = ['domingo', 'segunda', 'terça', 'quarta', 'quinta', 'sexta', 'sábado'];

export function deIso(iso: string): Date {
  const [a, m, d] = iso.slice(0, 10).split('-').map(Number);
  return new Date(a!, m! - 1, d!);
}

export function isoDe(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function diasAte(iso: string, hoje = new Date()): number {
  const h = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate());
  return Math.round((deIso(iso).getTime() - h.getTime()) / 86_400_000);
}

/** "hoje", "amanhã", "sexta" ou "dia 24/11". */
export function diaFalado(iso: string, hoje = new Date()): string {
  const n = diasAte(iso, hoje);
  if (n === 0) return 'hoje';
  if (n === 1) return 'amanhã';
  if (n > 1 && n < 7) return DIAS[deIso(iso).getDay()]!;
  return `dia ${dataCurta(iso)}`;
}

export function quandoVence(iso: string, hoje = new Date()): string {
  const n = diasAte(iso, hoje);
  if (n < 0) return `venceu há ${-n} ${-n === 1 ? 'dia' : 'dias'}`;
  return `vence ${diaFalado(iso, hoje)}`;
}

export const dataCurta = (iso: string) => iso.slice(0, 10).split('-').reverse().slice(0, 2).join('/');

export const reais = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
