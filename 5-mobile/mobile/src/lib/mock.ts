// Dados SIMULADOS para rodar sem API (regra da GS: nada de dado pessoal real).
// Datas relativas a hoje, para a demo sempre mostrar "vence sexta", "vence amanhã" etc.
import { isoDe } from './datas';
import type { Auditoria, Documento, Lembrete, Tarefa } from './tipos';

const daqui = (dias: number) => {
  const d = new Date();
  d.setDate(d.getDate() + dias);
  return isoDe(d);
};

export const mock = {
  lembretes: (): Lembrete[] => [
    { id: 1, titulo: 'Conta de água', dataVencimento: daqui(2), dataAviso: daqui(1), canal: 'esp32', concluido: false },
    { id: 2, titulo: 'IPTU parcela do mês', dataVencimento: daqui(5), dataAviso: daqui(4), canal: 'app', concluido: false },
    { id: 3, titulo: 'IPVA', dataVencimento: daqui(20), dataAviso: daqui(19), canal: 'app', concluido: false },
  ],
  documentos: (): Documento[] => [
    { id: 1, tarefaId: 1, tipo: 'boleto', valor: 87.4, vencimento: daqui(2), codigo: null },
    { id: 2, tarefaId: 2, tipo: 'agendamento', valor: null, vencimento: daqui(7), codigo: 'PT-2026-482913' },
    { id: 3, tarefaId: 3, tipo: 'protocolo', valor: null, vencimento: null, codigo: 'SP156-2026-0012345' },
  ],
  tarefas: (): Tarefa[] => [
    {
      id: 2, intencao: 'Quero agendar no Poupatempo', estado: 'concluida', criadaEm: new Date().toISOString(),
      servico: { nome: 'Agendamento no Poupatempo (simulado)' },
      passos: [
        { acao: 'aguardarUsuario', resultado: 'Você entrou na sua conta.', em: new Date().toISOString() },
        { acao: 'confirmar', resultado: 'Você confirmou.', em: new Date().toISOString() },
        { acao: 'concluir', resultado: 'Pronto! Seu agendamento está confirmado.', em: new Date().toISOString() },
      ],
    },
    {
      id: 1, intencao: 'Quero a 2ª via da conta de água', estado: 'falhou', criadaEm: new Date(Date.now() - 86_400_000).toISOString(),
      servico: { nome: '2ª via da conta de água' },
      passos: [{ acao: 'aguardarUsuario', resultado: 'Você saiu do login.', em: new Date().toISOString() }],
    },
  ],
  auditoria: (): Auditoria => ({
    riscoCidadao: 4,
    eventos: [
      { id: 1, tipo: 'PiiSemMascaraDaExtensao', nivelRisco: 'medio', detalhe: 'Encontrado: cpf', traducao: 'Bloqueamos o envio do seu CPF para fora do seu computador.', em: new Date().toISOString() },
      { id: 2, tipo: 'PiiNoChat', nivelRisco: 'baixo', detalhe: 'Encontrado: telefone', traducao: 'Você escreveu seu telefone na conversa. Escondemos antes de guardar.', em: new Date().toISOString() },
    ],
  }),
};
