// Protocolos de pedidos que a pessoa enviou sozinha no site do 156 e guardou no app.
// Ficam só no aparelho (banco local) e aparecem em Documentos junto com os da Paulinha.
import { guardar, lerGuardado } from './banco';
import type { Documento } from './tipos';

const CHAVE = 'protocolos-156';

export function protocolosGuardados(): Documento[] {
  return lerGuardado<Documento[]>(CHAVE) ?? [];
}

export function guardarProtocolo(codigo: string, titulo: string): Documento {
  const lista = protocolosGuardados();
  // Ids negativos para não colidir com os documentos que vêm da API.
  const novo: Documento = { id: -(lista.length + 1), tarefaId: 0, tipo: 'protocolo', valor: null, vencimento: null, codigo, titulo };
  guardar(CHAVE, [novo, ...lista]);
  return novo;
}
