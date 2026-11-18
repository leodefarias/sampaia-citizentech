export interface Lembrete {
  id: number;
  titulo: string;
  dataVencimento: string; // yyyy-MM-dd
  dataAviso: string;
  canal: 'app' | 'esp32';
  concluido: boolean;
}

export type TipoDocumento = 'boleto' | 'pix' | 'protocolo' | 'agendamento';

export interface Documento {
  id: number;
  tarefaId: number;
  tipo: TipoDocumento;
  valor: number | null;
  vencimento: string | null;
  codigo: string | null;
  /** Nome do pedido, quando a própria pessoa guardou o protocolo (ex.: "Buraco na rua"). */
  titulo?: string | null;
}

export type EstadoTarefa = 'planejando' | 'navegando' | 'aguardandoLogin' | 'aguardandoConfirmacao' | 'concluida' | 'falhou';

export interface Tarefa {
  id: number;
  intencao: string;
  estado: EstadoTarefa;
  criadaEm: string;
  servico: { nome: string } | null;
  passos: { acao: string; resultado: string | null; em: string }[];
}

export interface EventoAuditoria {
  id: number;
  tipo: string;
  nivelRisco: 'baixo' | 'medio' | 'alto';
  detalhe: string;
  traducao?: string | null;
  em: string;
}

export interface Auditoria {
  riscoCidadao: number;
  eventos: EventoAuditoria[];
}

export interface PrevisaoPrazo {
  faixa: string; // "até 7 dias" | "8 a 30 dias" | "mais de 30 dias"
  classe: string;
  fonte: 'modelo' | 'estimativa';
}
