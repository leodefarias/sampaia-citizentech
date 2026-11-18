import { guardar, lerGuardado } from './banco';
import { API_AGENTE, API_LEMBRETES, MODO_MOCK } from './config';
import { mock } from './mock';
import { protocolosGuardados } from './protocolos';
import type { Auditoria, Documento, Lembrete, PrevisaoPrazo, Tarefa } from './tipos';

async function buscar<T>(url: string, chave: string, simulado: () => T): Promise<T> {
  if (MODO_MOCK) return simulado();
  try {
    const r = await fetch(url);
    if (!r.ok) throw new Error(String(r.status));
    const dados = (await r.json()) as T;
    guardar(chave, dados);
    return dados;
  } catch (e) {
    // Sem internet: mostra o que foi guardado da última vez.
    const guardado = lerGuardado<T>(chave);
    if (guardado !== undefined) return guardado;
    throw e;
  }
}

async function enviar<T>(url: string, corpo: unknown): Promise<T> {
  const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(corpo) });
  if (!r.ok) throw new Error(String(r.status));
  return r.json() as Promise<T>;
}

export const api = {
  lembretes: () => buscar<Lembrete[]>(`${API_LEMBRETES}/api/lembretes`, 'lembretes', mock.lembretes),
  // Os protocolos que a pessoa guardou no app entram primeiro (são os mais recentes).
  documentos: async () => [...protocolosGuardados(), ...(await buscar<Documento[]>(`${API_AGENTE}/api/documentos`, 'documentos', mock.documentos))],
  tarefas: () => buscar<Tarefa[]>(`${API_AGENTE}/api/tarefas`, 'tarefas', mock.tarefas),
  auditoria: () => buscar<Auditoria>(`${API_AGENTE}/api/auditoria`, 'auditoria', mock.auditoria),

  concluirLembrete: (id: number) =>
    MODO_MOCK ? Promise.resolve() : enviar(`${API_LEMBRETES}/api/lembretes/${id}/concluir`, {}),

  /** Previsão do modelo de ML (via API do Agente). Envia só tipo, distrito e data: nada de foto ou coordenada. */
  preverPrazo: (p: { tipoServico: string; distrito: string | null }): Promise<PrevisaoPrazo> =>
    MODO_MOCK
      ? Promise.resolve({ faixa: '8 a 30 dias', classe: '8-30', fonte: 'estimativa' })
      : enviar(`${API_AGENTE}/api/previsao-prazo`, { ...p, dataAbertura: new Date().toISOString().slice(0, 10) }),
};
