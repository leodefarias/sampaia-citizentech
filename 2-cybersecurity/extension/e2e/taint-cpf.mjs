// Evidência de taint analysis: uma página com CPF, e-mail, telefone e senha (fictícios) é lida pela extensão.
// Prova que o dado é mascarado DENTRO do navegador: a API só recebe marcadores e registra o bloqueio.
// Pré-requisitos: APIs (5009/5010), portal simulado e `npm run build`. Uso: npm run e2e:taint
import { chromium } from 'playwright';
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const ext = fileURLToPath(new URL('../.output/chrome-mv3', import.meta.url));
const saida = fileURLToPath(new URL('../../security/evidencias/e2e-taint-cpf.txt', import.meta.url));
const CPF = '123.456.789-09';
const log = [];
const anotar = (s) => { console.log(s); log.push(s); };

const ctx = await chromium.launchPersistentContext('', {
  channel: 'chromium', headless: true, executablePath: process.env.CHROMIUM_PATH,
  args: [`--disable-extensions-except=${ext}`, `--load-extension=${ext}`],
});
let [sw] = ctx.serviceWorkers();
if (!sw) sw = await ctx.waitForEvent('serviceworker');
const id = sw.url().split('/')[2];

const site = await ctx.newPage();
await site.goto('http://localhost:5173/teste-pii.html');
const painel = await ctx.newPage();
await painel.goto(`chrome-extension://${id}/sidepanel.html`);
const tabId = await sw.evaluate(async () => (await chrome.tabs.query({ url: 'http://localhost:5173/*' }))[0].id);

// 1. O que o content script entrega (o que sairia do computador):
const retrato = await sw.evaluate((t) => chrome.tabs.sendMessage(t, { tipo: 'retrato' }), tabId);
anotar(`[fonte DOM] página contém CPF ${CPF}, e-mail, telefone e senha (fictícios)`);
anotar(`[sanitizador] tipos mascarados no navegador: ${retrato.pii.join(', ')}`);
anotar(`[retrato enviado] contém o CPF? ${retrato.conteudo.includes(CPF)} | contém [CPF]? ${retrato.conteudo.includes('[CPF]')} | contém a senha? ${retrato.conteudo.includes('senha-ficticia')}`);

// 2. Fluxo real: pedido com CPF digitado no chat + um passo na página.
await painel.evaluate(([t, cpf]) => chrome.runtime.sendMessage({ tipo: 'pedido', texto: `quero agendar no poupatempo simulado, meu CPF é ${cpf}`, tabId: t }), [tabId, CPF]);
await new Promise((r) => setTimeout(r, 6000));
const sessao = await painel.evaluate(async () => (await chrome.storage.session.get('sessao')).sessao);
anotar(`[sink storage] histórico salvo contém o CPF? ${JSON.stringify(sessao).includes(CPF)}`);

const tarefa = await (await fetch(`http://localhost:5009/api/tarefas/${sessao.tarefaId}`)).json();
anotar(`[sink banco] pedido gravado: "${tarefa.intencao}"`);
const aud = await (await fetch(`http://localhost:5009/api/auditoria?tarefaId=${sessao.tarefaId}`)).json();
for (const e of aud.eventos) anotar(`[evento] ${e.tipo} · ${e.nivelRisco} · ${e.detalhe} · "${e.traducao}"`);
anotar(`[servidor] chegou PII sem máscara? ${aud.eventos.some((e) => e.tipo === 'PiiSemMascaraDaExtensao')}`);
anotar(`[Risco Cidadão da tarefa] ${aud.riscoCidadao}`);

const ok = !retrato.conteudo.includes(CPF) && !JSON.stringify(sessao).includes(CPF) && !JSON.stringify(tarefa).includes(CPF) &&
  !aud.eventos.some((e) => e.tipo === 'PiiSemMascaraDaExtensao') && aud.eventos.some((e) => e.tipo === 'PiiBloqueadaNaExtensao');
anotar(ok ? 'RESULTADO: OK — o CPF não saiu do navegador.' : 'RESULTADO: FALHOU');
writeFileSync(saida, `Gerado em ${new Date().toISOString()} por extension/e2e/taint-cpf.mjs\n\n${log.join('\n')}\n`);
await ctx.close();
process.exit(ok ? 0 : 1);
