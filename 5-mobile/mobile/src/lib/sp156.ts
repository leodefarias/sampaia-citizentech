// Onde cada pedido do app nasce de verdade: o formulário oficial do SP156 (Prefeitura de São Paulo).
// Não há API pública para abrir pedido; o app leva a pessoa ao formulário certo e ela envia.
// Conferido no portal em 13/11/2026: páginas /portal/servicos/informacao?servico=ID com o botão
// "Solicitar Anonimamente", que abre /portal/servicos/solicitacao?servico=ID&anonimo=true
// (endereço, número, CEP e descrição; sem login e sem CPF; captcha no fim, feito pela pessoa).

const PORTAL = 'https://sp156.prefeitura.sp.gov.br/portal/servicos';

export type Envio156 =
  | { tipo: 'formulario'; servico: string; url: string; pagina: string }
  | { tipo: 'telefone'; motivo: string };

function formulario(id: number, servico: string): Envio156 {
  return {
    tipo: 'formulario',
    servico,
    url: `${PORTAL}/solicitacao?servico=${id}&anonimo=true`,
    pagina: `${PORTAL}/informacao?servico=${id}`,
  };
}

/** Rótulos iguais aos de app/pedido.tsx e de ROTULOS_APP em ml/preparar.py. */
export const ENVIO_156: Record<string, Envio156> = {
  'Buraco na rua': formulario(952, 'Tapa-buraco'),
  'Entulho ou lixo na rua': formulario(2641, 'Entulho e grandes objetos em via pública'),
  'Árvore (poda ou queda)': formulario(1071, 'Árvore: avaliação para poda ou remoção'),
  'Bueiro entupido': formulario(938, 'Limpeza de bueiros e bocas de lobo'),
  // Não achamos no portal um formulário on-line de "falta de varrição" da rua; o canal é a Central 156.
  'Falta de varrição': { tipo: 'telefone', motivo: 'Para falta de varrição, o pedido é feito pelo telefone 156.' },
};

/** Texto pronto para colar no campo "Descrição" do formulário (sem endereço nem dado pessoal). */
export function descricaoPronta(tipo: string, distrito?: string | null): string {
  return distrito ? `${tipo}, no bairro ${distrito}.` : `${tipo}.`;
}
