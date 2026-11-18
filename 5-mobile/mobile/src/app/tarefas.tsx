import { useQuery } from '@tanstack/react-query';
import { View } from 'react-native';
import { Cartao, Carregando, CirculoIcone, Selo, Tela, Texto, type NomeIcone, type TomSelo } from '@/components/ui';
import { api } from '@/lib/api';
import { dataCurta } from '@/lib/datas';
import { usePreferencias } from '@/lib/preferencias';
import type { EstadoTarefa } from '@/lib/tipos';

const ESTADOS: Record<EstadoTarefa, { nome: string; tom: TomSelo; icone: NomeIcone }> = {
  planejando: { nome: 'Planejando', tom: 'neutro', icone: 'relogio' },
  navegando: { nome: 'Em andamento', tom: 'aviso', icone: 'relogio' },
  aguardandoLogin: { nome: 'Esperando você entrar na conta', tom: 'aviso', icone: 'senha' },
  aguardandoConfirmacao: { nome: 'Esperando sua confirmação', tom: 'aviso', icone: 'maoLevantada' },
  concluida: { nome: 'Concluída', tom: 'sucesso', icone: 'certo' },
  falhou: { nome: 'Não terminou', tom: 'alerta', icone: 'fechar' },
};

export default function Tarefas() {
  const { cores } = usePreferencias();
  const { data, error, isLoading } = useQuery({ queryKey: ['tarefas'], queryFn: api.tarefas });
  const fundoDo = { sucesso: cores.sucessoFundo, alerta: cores.alertaFundo, aviso: cores.avisoFundo, neutro: cores.campo, destaque: cores.destaque };
  const corDo = { sucesso: cores.sucesso, alerta: cores.alerta, aviso: cores.aviso, neutro: cores.texto, destaque: cores.sobreDestaque };

  return (
    <Tela voltar titulo="O que já" destaque="resolvemos" icone="tarefas" subtitulo="Cada serviço que a Paulinha fez com você, do mais novo ao mais antigo.">
      <Carregando carregando={isLoading} erro={error} vazio={data?.length === 0} />
      {data?.map((t) => {
        const e = ESTADOS[t.estado];
        const passos = t.passos.filter((p) => p.resultado);
        return (
          <Cartao key={t.id} rotulo={`${t.servico?.nome ?? t.intencao}: ${e.nome}`}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
              <CirculoIcone nome={e.icone} fundo={fundoDo[e.tom]} cor={corDo[e.tom]} />
              <View style={{ flex: 1, gap: 2 }}>
                <Texto tamanho={20} negrito>{t.servico?.nome ?? t.intencao}</Texto>
                <Texto tamanho={15} suave>Pedido em {dataCurta(t.criadaEm)}</Texto>
              </View>
            </View>
            <Selo tom={e.tom} texto={e.nome} />
            {passos.length > 0 && (
              <View style={{ gap: 0, marginTop: 4 }}>
                {passos.map((p, i) => (
                  <View key={i} style={{ flexDirection: 'row', gap: 12 }}>
                    <View style={{ alignItems: 'center', width: 14 }}>
                      <View style={{ width: 12, height: 12, borderRadius: 6, marginTop: 7, backgroundColor: i === passos.length - 1 ? corDo[e.tom] : cores.suave }} />
                      {i < passos.length - 1 && <View style={{ flex: 1, width: 2, backgroundColor: cores.borda, marginVertical: 2 }} />}
                    </View>
                    <Texto tamanho={16} suave={i < passos.length - 1} style={{ flex: 1, paddingBottom: 10 }}>{p.resultado}</Texto>
                  </View>
                ))}
              </View>
            )}
          </Cartao>
        );
      })}
    </Tela>
  );
}
