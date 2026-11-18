// Regras visuais compartilhadas entre as telas (cor do selo e ícone de cada conta).
import type { NomeIcone } from '@/components/Icone';
import type { TomSelo } from '@/components/ui';
import { diasAte } from './datas';

/** Vencido ou até 2 dias: alerta. Até 7 dias: aviso. Depois: neutro. */
export function tomDoPrazo(iso: string): TomSelo {
  const n = diasAte(iso);
  if (n <= 2) return 'alerta';
  if (n <= 7) return 'aviso';
  return 'neutro';
}

export function iconeDaConta(titulo: string): NomeIcone {
  const t = titulo.toLowerCase();
  if (t.includes('água') || t.includes('agua') || t.includes('sabesp')) return 'agua';
  if (t.includes('iptu')) return 'casa';
  if (t.includes('ipva') || t.includes('carro')) return 'carro';
  if (t.includes('poupatempo') || t.includes('agend')) return 'calendario';
  return 'boleto';
}

export function saudacao(agora = new Date()): string {
  const h = agora.getHours();
  if (h < 12) return 'Bom dia!';
  if (h < 18) return 'Boa tarde!';
  return 'Boa noite!';
}

export const comMaiuscula = (s: string) => s.charAt(0).toUpperCase() + s.slice(1);
