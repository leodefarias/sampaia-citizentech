import { EXCLUIR_PAGAMENTO, ehLogin, ehPaginaPagamento, PADROES_HOST } from '@/lib/config';
import { esperarQuieto } from '@/lib/espera';
import { mascarar } from '@/lib/pii';
import type { Acao, PedidoConteudo, Retrato } from '@/lib/tipos';

const MAX_TEXTO = 6000;
const MAX_ELEMENTOS = 80;
// Além de campos, botões e links: clicáveis feitos com outras tags, e os dias com vaga do calendário
// do agendamento do Poupatempo (<td class="day event">), que só se distinguem pela classe.
const CLICAVEIS = 'input:not([type=hidden]), select, textarea, button, a[href], [role=button], [onclick], td.event';

function visivel(el: Element) {
  const r = el.getBoundingClientRect();
  return r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== 'hidden';
}

function seletor(el: Element): string {
  if (el.id) return '#' + CSS.escape(el.id);
  const nome = el.getAttribute('name');
  if (nome) return `${el.tagName.toLowerCase()}[name="${nome}"]`;
  const partes: string[] = [];
  for (let e: Element | null = el; e && e !== document.body; e = e.parentElement) {
    const irmaos = e.parentElement ? [...e.parentElement.children].filter((x) => x.tagName === e!.tagName) : [];
    partes.unshift(`${e.tagName.toLowerCase()}:nth-of-type(${irmaos.indexOf(e) + 1})`);
  }
  return 'body > ' + partes.join(' > ');
}

function rotulo(el: Element): string {
  const id = el.id && document.querySelector(`label[for="${CSS.escape(el.id)}"]`)?.textContent;
  const texto = id || el.getAttribute('aria-label') || el.getAttribute('placeholder') || (el as HTMLElement).innerText || el.getAttribute('value') || '';
  return texto.replace(/\s+/g, ' ').trim().slice(0, 60);
}

function descrever(el: Element): string {
  const tipo = el.tagName.toLowerCase() + (el instanceof HTMLInputElement ? `:${el.type}` : '');
  let valor = '';
  if (el instanceof HTMLInputElement && el.type === 'password') valor = '[senha oculta]';
  else if (el instanceof HTMLInputElement || el instanceof HTMLSelectElement || el instanceof HTMLTextAreaElement) valor = el.value;
  const opcoes = el instanceof HTMLSelectElement ? ` opções: ${[...el.options].map((o) => o.text).join(' | ')}` : '';
  const vaga = el.matches('td.event') ? ' [dia com vaga]' : '';
  return `${seletor(el)} (${tipo}) "${rotulo(el)}" = "${valor}"${opcoes}${vaga}`;
}

/** Fonte da taint analysis: o DOM. Sai daqui sempre mascarado. */
/** Liga um MutationObserver no corpo da página (usado para esperar o site terminar de montar). */
function observarPagina(aoMudar: () => void) {
  const o = new MutationObserver(aoMudar);
  o.observe(document.body, { childList: true, subtree: true, characterData: true });
  return () => o.disconnect();
}

async function retrato(): Promise<Retrato> {
  const base = { url: location.href, titulo: document.title };
  if (ehPaginaPagamento(location.href)) return { ...base, conteudo: '[PÁGINA DE PAGAMENTO: a Paulinha não lê]' };
  if (ehLogin(location.href)) return { ...base, conteudo: '[TELA DE LOGIN: a Paulinha não lê]' };

  // Sites como o SP156 montam o formulário depois do "load": ler cedo demais mostra a página vazia.
  await esperarQuieto(observarPagina);

  const texto = document.body.innerText.slice(0, MAX_TEXTO);
  const elementos = [...document.querySelectorAll(CLICAVEIS)]
    .filter(visivel)
    .slice(0, MAX_ELEMENTOS)
    .map(descrever);
  const m = mascarar(`TEXTO:\n${texto}\n\nELEMENTOS:\n${elementos.join('\n')}`);
  return { ...base, conteudo: m.texto, pii: m.encontrados };
}

const espera = (ms: number) => new Promise((r) => setTimeout(r, ms));

/** Destaca o elemento antes de agir, para a pessoa ver o que vai acontecer. */
async function executar(acao: Acao): Promise<{ ok: boolean }> {
  const el = acao.seletor ? (document.querySelector(acao.seletor) as HTMLElement | null) : null;
  if (!el) return { ok: false };

  el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  const antes = el.style.outline;
  el.style.outline = '4px solid #ff9800';
  el.style.outlineOffset = '3px';
  await espera(900);
  el.style.outline = antes;

  if (acao.acao === 'clicar') {
    // Link que abre aba nova (ex.: resultados da busca do Poupatempo): a Paulinha só acompanha a aba dela,
    // então abre na mesma aba. Sem isso cada clique abria uma aba e a tarefa não andava.
    const link = el.closest('a[target]');
    if (link && link.getAttribute('target') !== '_self') link.setAttribute('target', '_self');
    el.click();
  }
  else if (acao.acao === 'preencher') preencher(el, acao.valor ?? '');
  return { ok: true };
}

/** Funciona com React e afins: usa o setter nativo e dispara input/change. */
function preencher(el: HTMLElement, valor: string) {
  const proto = el instanceof HTMLSelectElement ? HTMLSelectElement.prototype : el instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
  if (el instanceof HTMLSelectElement) {
    const opcao = [...el.options].find((o) => o.value === valor || o.text === valor);
    if (opcao) valor = opcao.value;
  }
  Object.getOwnPropertyDescriptor(proto, 'value')?.set?.call(el, valor);
  el.dispatchEvent(new Event('input', { bubbles: true }));
  el.dispatchEvent(new Event('change', { bubbles: true }));
}

export default defineContentScript({
  // Mesmos hosts do manifest. gov.br e telas de pagamento ficam de fora.
  matches: PADROES_HOST,
  excludeMatches: EXCLUIR_PAGAMENTO,
  main() {
    browser.runtime.onMessage.addListener((p: PedidoConteudo, _s, responder) => {
      if (p.tipo === 'retrato') retrato().then(responder);
      else if (p.tipo === 'executar') executar(p.acao).then(responder);
      return true; // resposta assíncrona
    });
  },
});
