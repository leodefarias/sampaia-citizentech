import { useQuery } from '@tanstack/react-query';
import { router, type Href } from 'expo-router';
import { useEffect } from 'react';
import { View } from 'react-native';
import { Botao, Cartao, Carregando, CirculoIcone, Selo, Tela, Texto, Titulo, type NomeIcone } from '@/components/ui';
import { api } from '@/lib/api';
import { comMaiuscula, iconeDaConta, saudacao, tomDoPrazo } from '@/lib/aparencia';
import { MODO_MOCK } from '@/lib/config';
import { diasAte, quandoVence } from '@/lib/datas';
import { agendarAvisos } from '@/lib/notificacoes';
import { usePreferencias } from '@/lib/preferencias';
import type { Lembrete } from '@/lib/tipos';

const ATALHOS: { titulo: string; rota: Href; dica: string; icone: NomeIcone }[] = [
  { titulo: 'Documentos', rota: '/documentos', dica: 'Boletos, agendamentos e protocolos', icone: 'documentos' },
  { titulo: 'Pedir conserto', rota: '/pedido', dica: 'Buraco, lixo ou árvore na rua', icone: 'pedido' },
  { titulo: 'O que já resolvemos', rota: '/tarefas', dica: 'Tudo o que a Paulinha fez', icone: 'tarefas' },
  { titulo: 'Segurança', rota: '/seguranca', dica: 'Como seus dados ficam protegidos', icone: 'seguranca' },
];

function Cabecalho() {
  const { cores } = usePreferencias();
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }} accessible accessibilityLabel={`${saudacao()} Eu sou a Paulinha, sua ajudante nos serviços de São Paulo.`}>
      <CirculoIcone nome="paulinha" tamanho={56} fundo={cores.destaque} cor={cores.sobreDestaque} />
      <View style={{ flex: 1 }}>
        <Texto tamanho={15} suave>{saudacao()}</Texto>
        <Texto tamanho={20} negrito>Eu sou a Paulinha</Texto>
      </View>
    </View>
  );
}

function Principal({ l }: { l: Lembrete }) {
  const { cores, falar, lerEmVoz } = usePreferencias();
  const quando = quandoVence(l.dataVencimento);
  const frase = `${l.titulo} ${quando}`;
  return (
    <Cartao tom="escuro" rotulo={`Mais próximo: ${frase}`}>
      <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
        <CirculoIcone nome={iconeDaConta(l.titulo)} fundo="rgba(255,255,255,0.12)" cor={cores.sobreHeroi} />
        <Selo tom="destaque" icone="aviso" texto={diasAte(l.dataVencimento) <= 2 ? 'Atenção' : 'A próxima'} />
      </View>
      <View style={{ gap: 2, marginTop: 4 }}>
        <Texto tamanho={19} suave>{l.titulo}</Texto>
        <Texto tamanho={34} negrito style={{ letterSpacing: -0.6 }}>{comMaiuscula(quando)}</Texto>
      </View>
      {lerEmVoz && <Botao variante="destaque" icone="ouvir" titulo="Ouvir" rotulo={`Ouvir: ${frase}`} onPress={() => falar(frase)} />}
    </Cartao>
  );
}

function Proximo({ l }: { l: Lembrete }) {
  const { falar, lerEmVoz } = usePreferencias();
  const quando = quandoVence(l.dataVencimento);
  const frase = `${l.titulo} ${quando}`;
  return (
    <Cartao rotulo={frase}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
        <CirculoIcone nome={iconeDaConta(l.titulo)} />
        <View style={{ flex: 1, gap: 6 }}>
          <Texto tamanho={20} negrito>{l.titulo}</Texto>
          <Selo tom={tomDoPrazo(l.dataVencimento)} texto={comMaiuscula(quando)} />
        </View>
      </View>
      {lerEmVoz && <Botao compacto variante="secundario" icone="ouvir" titulo="Ouvir" rotulo={`Ouvir: ${frase}`} onPress={() => falar(frase)} />}
    </Cartao>
  );
}

export default function Inicio() {
  const { cores, escala } = usePreferencias();
  const { data, error, isLoading } = useQuery({ queryKey: ['lembretes'], queryFn: api.lembretes });
  const abertos = (data ?? []).filter((l) => !l.concluido).sort((a, b) => a.dataVencimento.localeCompare(b.dataVencimento));
  const [primeiro, ...outros] = abertos;

  useEffect(() => {
    if (data) agendarAvisos(data).catch(() => {});
  }, [data]);

  return (
    <Tela topo={<Cabecalho />} titulo="Contas" destaque="em dia" icone="calendario" subtitulo="Eu aviso antes de cada vencimento, para nada ficar para trás.">
      {MODO_MOCK && <Selo tom="neutro" icone="aviso" texto="Demonstração: dados simulados" />}
      <Carregando carregando={isLoading} erro={error} vazio={!!data && abertos.length === 0} />
      {primeiro && <Principal l={primeiro} />}
      {outros.length > 0 && <Titulo>Próximas contas</Titulo>}
      {outros.map((l) => <Proximo key={l.id} l={l} />)}

      <Titulo>O que vamos resolver?</Titulo>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 12 }}>
        {ATALHOS.map((a) => (
          <Cartao
            key={a.titulo}
            onPress={() => router.push(a.rota)}
            rotulo={a.titulo}
            dica={a.dica}
            style={{ width: escala > 1.3 ? '100%' : '47.5%', flexGrow: 1, padding: 18 }}
          >
            <CirculoIcone nome={a.icone} tamanho={52} fundo={cores.destaque} cor={cores.sobreDestaque} />
            <View style={{ gap: 2 }}>
              <Texto tamanho={18} negrito>{a.titulo}</Texto>
              <Texto tamanho={15} suave>{a.dica}</Texto>
            </View>
          </Cartao>
        ))}
      </View>

      <Cartao tom="destaque" rotulo="Vai pagar ou agendar? Chame a Paulinha no Chrome do computador. O que ela fizer aparece aqui.">
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
          <CirculoIcone nome="computador" fundo={cores.cartao} cor={cores.texto} />
          <View style={{ flex: 1, gap: 2 }}>
            <Texto tamanho={18} negrito>Vai pagar ou agendar?</Texto>
            <Texto tamanho={16}>Chame a Paulinha no Chrome do computador. O que ela fizer aparece aqui.</Texto>
          </View>
        </View>
      </Cartao>
    </Tela>
  );
}
