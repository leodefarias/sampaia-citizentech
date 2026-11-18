import { Pressable, Switch, Text, View } from 'react-native';
import { Botao, Cartao, CirculoIcone, Tela, Texto, Titulo, type NomeIcone } from '@/components/ui';
import { usePreferencias, type TamanhoLetra } from '@/lib/preferencias';

const TAMANHOS: { valor: TamanhoLetra; nome: string; amostra: number }[] = [
  { valor: 'normal', nome: 'Normal', amostra: 20 },
  { valor: 'grande', nome: 'Grande', amostra: 26 },
  { valor: 'enorme', nome: 'Enorme', amostra: 32 },
];

function Interruptor({ titulo, descricao, icone, valor, mudar }: { titulo: string; descricao: string; icone: NomeIcone; valor: boolean; mudar: (v: boolean) => void }) {
  const { cores } = usePreferencias();
  return (
    <Pressable onPress={() => mudar(!valor)} accessibilityRole="switch" accessibilityState={{ checked: valor }} accessibilityLabel={titulo} accessibilityHint={descricao}
      style={{ flexDirection: 'row', alignItems: 'center', gap: 14, minHeight: 64 }}>
      <CirculoIcone nome={icone} tamanho={48} />
      <View style={{ flex: 1, gap: 2 }}>
        <Texto tamanho={19} negrito>{titulo}</Texto>
        <Texto tamanho={15} suave>{descricao}</Texto>
      </View>
      <Switch
        value={valor}
        onValueChange={mudar}
        trackColor={{ true: cores.primaria, false: cores.borda }}
        thumbColor={valor ? cores.destaque : '#FFFFFF'}
        {...{ activeThumbColor: cores.destaque }} // react-native-web ignora thumbColor quando ligado
        importantForAccessibility="no"
        style={{ transform: [{ scale: 1.25 }] }}
      />
    </Pressable>
  );
}

export default function Ajustes() {
  const p = usePreferencias();
  const { cores } = p;
  return (
    <Tela titulo="Seus" destaque="ajustes" icone="ajustes" subtitulo="Letra, cores e voz do jeito que fica melhor para você.">
      <Titulo>Tamanho da letra</Titulo>
      <View accessibilityRole="radiogroup" style={{ flexDirection: 'row', gap: 10 }}>
        {TAMANHOS.map((t) => {
          const escolhido = p.tamanho === t.valor;
          const cor = escolhido ? cores.sobrePrimaria : cores.texto;
          return (
            <Pressable
              key={t.valor}
              onPress={() => p.mudar({ tamanho: t.valor })}
              accessibilityRole="radio"
              accessibilityState={{ checked: escolhido }}
              accessibilityLabel={`Letra ${t.nome}`}
              style={({ pressed }) => ({
                flex: 1, minHeight: 104, borderRadius: 24, alignItems: 'center', justifyContent: 'center', gap: 4,
                backgroundColor: escolhido ? cores.primaria : cores.cartao,
                borderWidth: 2, borderColor: escolhido ? cores.primaria : p.altoContraste ? cores.borda : cores.cartao,
                transform: [{ scale: pressed ? 0.97 : 1 }],
                ...(!p.altoContraste && !escolhido ? { boxShadow: '0px 6px 24px rgba(21, 23, 28, 0.07)' } : null),
              })}
            >
              <Text style={{ fontSize: t.amostra, fontWeight: '800', color: cor }}>Aa</Text>
              <Text style={{ fontSize: 16, fontWeight: '700', color: cor }}>{t.nome}</Text>
            </Pressable>
          );
        })}
      </View>

      <Titulo>Ver e ouvir</Titulo>
      <Cartao>
        <Interruptor icone="contraste" titulo="Alto contraste" descricao="Fundo preto e letras bem fortes, mais fácil de ler" valor={p.altoContraste} mudar={(v) => p.mudar({ altoContraste: v })} />
        <View style={{ height: 1, backgroundColor: cores.borda }} />
        <Interruptor icone="ouvir" titulo="Ouvir em voz alta" descricao="A Paulinha lê as contas e os documentos para você" valor={p.lerEmVoz} mudar={(v) => p.mudar({ lerEmVoz: v })} />
      </Cartao>
      <Botao variante="secundario" icone="ouvir" titulo="Testar a voz" onPress={() => p.falar('Olá! Eu leio os vencimentos para você.')} />
    </Tela>
  );
}
