import { useQuery } from '@tanstack/react-query';
import { View } from 'react-native';
import { Cartao, Carregando, CirculoIcone, Icone, Selo, Tela, Texto, Titulo, type NomeIcone, type TomSelo } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta } from '@/lib/datas';
import { usePreferencias } from '@/lib/preferencias';

// Risco Cidadão = soma ponderada dos eventos (baixo 1, médio 3, alto 5). Faixas para leitura simples.
function nivel(risco: number) {
  if (risco === 0) return { faixa: 0, titulo: 'Tudo tranquilo', texto: 'Nenhum dado seu precisou ser bloqueado.' };
  if (risco < 5) return { faixa: 1, titulo: 'Atenção baixa', texto: 'Bloqueamos alguns dados por precaução. Nada saiu do seu computador.' };
  if (risco < 15) return { faixa: 2, titulo: 'Atenção média', texto: 'Vários dados pessoais apareceram nas páginas. Todos foram escondidos.' };
  return { faixa: 3, titulo: 'Atenção alta', texto: 'Muitos dados sensíveis apareceram. Evite digitar documentos na conversa.' };
}

const NIVEIS: Record<'baixo' | 'medio' | 'alto', { nome: string; tom: TomSelo }> = {
  baixo: { nome: 'Nível baixo', tom: 'sucesso' },
  medio: { nome: 'Nível médio', tom: 'aviso' },
  alto: { nome: 'Nível alto', tom: 'alerta' },
};

const GARANTIAS: { icone: NomeIcone; texto: string }[] = [
  { icone: 'senha', texto: 'A Paulinha nunca vê sua senha. Você entra na conta sozinho.' },
  { icone: 'maoLevantada', texto: 'Nada é pago ou confirmado sem você dizer “Confirmo”.' },
  { icone: 'cadeado', texto: 'CPF e outros documentos são escondidos antes de sair do computador.' },
];

export default function Seguranca() {
  const { cores } = usePreferencias();
  const { data, error, isLoading } = useQuery({ queryKey: ['auditoria'], queryFn: api.auditoria });
  const n = nivel(data?.riscoCidadao ?? 0);

  return (
    <Tela voltar titulo="Seus dados" destaque="protegidos" icone="seguranca" subtitulo="Veja o que a Paulinha escondeu e bloqueou para cuidar de você.">
      <Carregando carregando={isLoading} erro={error} />
      {data && (
        <Cartao tom="escuro" rotulo={`Risco Cidadão: ${n.titulo}. ${n.texto}`}>
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
            <CirculoIcone nome="seguranca" fundo={cores.destaque} cor={cores.sobreDestaque} />
            <Selo tom="destaque" texto="Risco Cidadão" />
          </View>
          <Texto tamanho={30} negrito style={{ letterSpacing: -0.5 }}>{n.titulo}</Texto>
          <View style={{ flexDirection: 'row', gap: 6 }}>
            {[0, 1, 2, 3].map((i) => (
              <View key={i} style={{ flex: 1, height: 10, borderRadius: 5, backgroundColor: i <= n.faixa ? cores.destaque : 'rgba(255,255,255,0.18)' }} />
            ))}
          </View>
          <Texto suave>{n.texto}</Texto>
        </Cartao>
      )}

      <Titulo>O que a Paulinha promete</Titulo>
      <Cartao>
        {GARANTIAS.map((g) => (
          <View key={g.icone} style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
            <CirculoIcone nome={g.icone} tamanho={48} fundo={cores.sucessoFundo} cor={cores.sucesso} />
            <Texto tamanho={17} style={{ flex: 1 }}>{g.texto}</Texto>
          </View>
        ))}
      </Cartao>

      {!!data?.eventos.length && <Titulo>O que aconteceu</Titulo>}
      {data?.eventos.map((e) => (
        <Cartao key={e.id} rotulo={e.traducao ?? e.detalhe}>
          <View style={{ flexDirection: 'row', gap: 12, alignItems: 'flex-start' }}>
            <Icone nome="cadeado" tamanho={24} cor={cores.texto} />
            <Texto style={{ flex: 1 }}>{e.traducao ?? e.detalhe}</Texto>
          </View>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <Selo tom={NIVEIS[e.nivelRisco].tom} texto={NIVEIS[e.nivelRisco].nome} />
            <Texto suave tamanho={15}>{dataCurta(e.em)}</Texto>
          </View>
        </Cartao>
      ))}
    </Tela>
  );
}
