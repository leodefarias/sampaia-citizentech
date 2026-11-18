import { api, ErroApi } from '@/lib/api';
import { ehLogin, ehLoginGovBr, hostPermitido, voltouDoLogin } from '@/lib/config';
import { mascarar } from '@/lib/pii';
import type { Acao, PedidoPainel, Retrato, Sessao } from '@/lib/tipos';

const MAX_PASSOS = 30;
const MAX_ERROS = 3;
const VAZIA: Sessao = { plano: [], mensagens: [], passos: 0, erros: 0 };

/**
 * Valores reais pedidos ao usuário para trocar marcadores ([SQL]) na hora de preencher.
 * Ficam só na memória do service worker: nunca vão para storage, log nem API.
 */
const cofre = new Map<string, string>();

// ---- estado (chrome.storage.session, sobrevive ao service worker dormir) ----

async function ler(): Promise<Sessao> {
  const { sessao } = await browser.storage.session.get('sessao');
  return (sessao as Sessao) ?? { ...VAZIA };
}

async function salvar(mudanca: Partial<Sessao>) {
  const s = { ...(await ler()), ...mudanca };
  // Sink storage: mensagens são mascaradas antes de gravar.
  const achados: string[] = [];
  s.mensagens = s.mensagens.map((m) => {
    const r = mascarar(m.texto);
    achados.push(...r.encontrados);
    return { ...m, texto: r.texto };
  });
  if (achados.length) void api.reportar(s.tarefaId, 'storage', achados);
  await browser.storage.session.set({ sessao: s });
  return s;
}

async function dizer(texto: string, de: 'paulinha' | 'voce' = 'paulinha') {
  const s = await ler();
  await salvar({ mensagens: [...s.mensagens, { de, texto }] });
}

// ---- ciclo da tarefa ----

let timer: ReturnType<typeof setTimeout> | undefined;
let rodando = false;

/** Agenda o próximo passo; chamadas seguidas se juntam numa só (debounce). */
function agendarPasso(ms: number) {
  clearTimeout(timer);
  timer = setTimeout(() => void passo(), ms);
}

async function passo() {
  if (rodando) return agendarPasso(500);
  rodando = true;
  try {
    const s = await ler();
    if (s.estado !== 'navegando' || !s.tarefaId || !s.tabId || s.pergunta || s.esperandoUsuario) return;
    if (s.passos >= MAX_PASSOS) return falhar('Demorei demais nesta tarefa.');

    const tab = await browser.tabs.get(s.tabId).catch(() => undefined);
    if (!tab) return falhar('A aba do serviço foi fechada.');
    if (ehLogin(tab.url)) return;

    const retrato = await tirarRetrato(s.tabId, tab.url);
    if (retrato.pii?.length) void api.reportar(s.tarefaId, 'llm', retrato.pii);
    const acao = await api.proximoPasso(s.tarefaId, retrato);
    await salvar({ passos: s.passos + 1 });
    await executar(acao);
  } catch (e) {
    await falhar(e instanceof ErroApi ? e.message : 'Algo deu errado na página.');
  } finally {
    rodando = false;
  }
}

async function tirarRetrato(tabId: number, url?: string): Promise<Retrato> {
  // Fora dos hosts liberados o navegador não deixa ler; mandamos só a URL.
  if (!hostPermitido(url)) return { url: url ?? '', titulo: '', conteudo: '' };
  try {
    return await browser.tabs.sendMessage(tabId, { tipo: 'retrato' });
  } catch {
    return { url: url ?? '', titulo: '', conteudo: '' };
  }
}

const MARCADOR = /^\[([A-Z]+)\]$/;
/** Formato esperado de cada número pedido ao usuário, com a explicação simples se estiver errado. */
const FORMATOS: Record<string, { re: RegExp; ajuda: string }> = {
  SQL: { re: /^\d{3}\.?\d{3}\.?\d{4}-?\d$/, ajuda: 'O número SQL tem 11 números, assim: 000.000.0000-0. Ele aparece no carnê do IPTU, perto do endereço do imóvel.' },
  RENAVAM: { re: /^\d{9,11}$/, ajuda: 'O Renavam tem 11 números e está no documento do veículo (CRLV).' },
  PLACA: { re: /^[A-Za-z]{3}-?\d[A-Za-z0-9]\d{2}$/, ajuda: 'A placa tem 3 letras e 4 números ou letras, como ABC1D23.' },
};

// Já com a preposição ("Preciso do ..."), para a frase sair certa.
const NOMES: Record<string, string> = { SQL: 'do número SQL do seu imóvel (está no carnê do IPTU)', RENAVAM: 'do Renavam do veículo', PLACA: 'da placa do veículo', FORNECIMENTO: 'do número de fornecimento da conta de água' };

async function executar(acao: Acao) {
  const s = await ler();
  await dizer(acao.explicacao);

  switch (acao.acao) {
    case 'navegar':
      if (!hostPermitido(acao.valor ?? undefined)) return falhar('A Paulinha só abre os sites oficiais da lista.');
      await browser.tabs.update(s.tabId!, { url: acao.valor! });
      return; // tabs.onUpdated chama o próximo passo quando a página carregar

    case 'clicar':
    case 'preencher': {
      const marcador = acao.valor?.match(MARCADOR)?.[1];
      if (marcador && !cofre.has(marcador)) {
        await salvar({ pergunta: marcador, pendente: acao });
        return dizer(`Preciso ${NOMES[marcador] ?? `do ${marcador}`}. Pode digitar aqui embaixo? Esse número fica só no seu computador.`);
      }
      return agir(acao);
    }

    case 'aguardarUsuario':
      await salvar({ esperandoUsuario: true });
      return;

    case 'confirmar':
      await salvar({ estado: 'aguardandoConfirmacao', pendente: acao });
      return dizer('Posso continuar? Toque em "Confirmo" para eu seguir ou em "Não" para parar.');

    case 'concluir':
      await salvar({ estado: 'concluida' });
      return dizer('Tudo certo! Guardei o resultado em Documentos no aplicativo.');
  }
}

/** Executa a ação na página (troca o marcador pelo valor real só aqui, dentro do computador). */
async function agir(acao: Acao) {
  const s = await ler();
  const marcador = acao.valor?.match(MARCADOR)?.[1];
  const real = marcador ? { ...acao, valor: cofre.get(marcador)! } : acao;
  const r = await browser.tabs.sendMessage(s.tabId!, { tipo: 'executar', acao: real }).catch(() => ({ ok: false }));
  if (r?.ok) {
    await salvar({ erros: 0 });
  } else {
    const erros = s.erros + 1;
    await salvar({ erros });
    if (erros >= MAX_ERROS) return falhar('Não encontrei o botão certo na página.');
  }
  agendarPasso(1500);
}

async function falhar(motivo: string) {
  const s = await ler();
  if (s.tarefaId && s.estado !== 'falhou' && s.estado !== 'concluida') await api.falhar(s.tarefaId, motivo).catch(() => {});
  await salvar({ estado: 'falhou', pendente: undefined, pergunta: undefined, esperandoUsuario: false });
  await dizer(`${motivo} Sem problema: abaixo está o passo a passo para você fazer com calma.`);
}

// ---- mensagens do painel ----

async function tratar(p: PedidoPainel) {
  const s = await ler();
  switch (p.tipo) {
    case 'pedido': {
      cofre.clear();
      await salvar({ ...VAZIA, mensagens: [{ de: 'voce', texto: p.texto }] });
      try {
        const [ativa] = await browser.tabs.query({ active: true, lastFocusedWindow: true });
        const tabId = p.tabId ?? ativa?.id;
        const { tarefa, plano } = await api.criarTarefa(p.texto);
        await salvar({ tarefaId: tarefa.id, tabId, estado: 'navegando', plano });
        await dizer(`Combinado! Vou fazer assim:\n${plano.map((x, i) => `${i + 1}. ${x}`).join('\n')}`);
        agendarPasso(0);
      } catch (e) {
        await dizer(e instanceof ErroApi ? e.message : 'Não consegui falar com o servidor.');
      }
      return;
    }
    case 'resposta': {
      if (!s.pergunta || !s.pendente) return;
      const formato = FORMATOS[s.pergunta];
      if (formato && !formato.re.test(p.valor.trim())) {
        await dizer('(número com formato errado)', 'voce');
        return dizer(`Esse número não parece certo. ${formato.ajuda} Pode digitar de novo?`);
      }
      cofre.set(s.pergunta, p.valor.trim());
      await dizer('(informado)', 'voce'); // o valor em si não vai para o histórico
      const acao = s.pendente;
      await salvar({ pergunta: undefined, pendente: undefined });
      return agir(acao);
    }
    case 'confirmar': {
      if (s.estado !== 'aguardandoConfirmacao' || !s.tarefaId) return;
      await dizer(p.sim ? 'Confirmo' : 'Não', 'voce');
      await api.confirmar(s.tarefaId, p.sim);
      if (!p.sim) {
        await salvar({ estado: 'falhou', pendente: undefined });
        return dizer('Tudo bem, parei aqui e nada foi gerado.');
      }
      const acao = s.pendente;
      await salvar({ estado: 'navegando', pendente: undefined });
      return acao?.seletor ? agir({ ...acao, acao: 'clicar' }) : agendarPasso(0);
    }
    case 'continuar':
      await salvar({ esperandoUsuario: false });
      return agendarPasso(0);
    case 'cancelar':
      return falhar(s.estado === 'aguardandoLogin' ? 'Você saiu do login.' : 'Você cancelou.');
    case 'nova':
      cofre.clear();
      await browser.storage.session.set({ sessao: { ...VAZIA } });
  }
}

export default defineBackground(() => {
  browser.sidePanel.setPanelBehavior({ openPanelOnActionClick: true }).catch(() => {});

  browser.runtime.onMessage.addListener((p: PedidoPainel) => {
    void tratar(p);
  });

  // Detecção do login: a URL muda para o gov.br (onde a extensão não tem permissão) e depois volta.
  browser.tabs.onUpdated.addListener(async (tabId, info, tab) => {
    const s = await ler();
    if (tabId !== s.tabId || !s.tarefaId) return;

    // info.url só vem uma vez por navegação (status loading/complete repetem a mesma URL).
    if (info.url && ehLogin(info.url) && s.estado === 'navegando') {
      const gov = ehLoginGovBr(info.url);
      await api.pausar(s.tarefaId).catch(() => {});
      await salvar({ estado: 'aguardandoLogin', loginGovBr: gov });
      return dizer(`${gov ? 'Chegamos no login do gov.br' : 'Chegamos na tela de entrada do site'}. Essa parte é com você: eu não consigo ver essa tela, e é por segurança.`);
    }
    if (info.status !== 'complete') return;
    if (s.estado === 'aguardandoLogin' && voltouDoLogin(tab.url)) {
      await api.retomar(s.tarefaId).catch(() => {});
      await salvar({ estado: 'navegando' });
      await dizer('Você entrou! Agora eu continuo daqui.');
      return agendarPasso(800);
    }
    if (s.estado === 'navegando') agendarPasso(800);
  });

  browser.tabs.onRemoved.addListener(async (tabId) => {
    const s = await ler();
    if (tabId === s.tabId && (s.estado === 'navegando' || s.estado === 'aguardandoLogin')) await falhar('A aba do serviço foi fechada.');
  });
});
