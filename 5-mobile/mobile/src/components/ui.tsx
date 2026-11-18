// Componentes base. Visual moderno (cartões arredondados, botões em pílula), sem abrir mão da acessibilidade:
// fonte base 18 (x escala), alvos >= 56 dp, rótulos sempre visíveis, papéis e estados para leitor de tela.
import { router } from 'expo-router';
import { createContext, useContext, type ReactNode } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View, type StyleProp, type TextProps, type ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { usePreferencias, type Cores } from '@/lib/preferencias';
import { Icone, type NomeIcone } from './Icone';

export { Icone, type NomeIcone } from './Icone';

const SOMBRA = '0px 6px 24px rgba(21, 23, 28, 0.07)';

/** Cor do texto herdada do cartão (texto claro dentro do cartão escuro, por exemplo). */
const CorDoTexto = createContext<{ texto: string; suave: string } | null>(null);

/**
 * Tela com cabeçalho em cartão: ícone da tela num círculo verde-limão e o título com a parte principal
 * marcada como caneta marca-texto (`destaque`). Ex.: titulo="Contas" destaque="em dia".
 */
export function Tela({ children, titulo, destaque, subtitulo, icone, voltar, topo }: {
  children: ReactNode; titulo?: string; destaque?: string; subtitulo?: string; icone?: NomeIcone; voltar?: boolean; topo?: ReactNode;
}) {
  const { cores } = usePreferencias();
  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: cores.fundo }} edges={['top']}>
      <ScrollView contentContainerStyle={estilos.tela}>
        {voltar && <BotaoVoltar />}
        {topo}
        {titulo && <Cabecalho titulo={titulo} destaque={destaque} subtitulo={subtitulo} icone={icone} />}
        {children}
      </ScrollView>
    </SafeAreaView>
  );
}

function Cabecalho({ titulo, destaque, subtitulo, icone }: { titulo: string; destaque?: string; subtitulo?: string; icone?: NomeIcone }) {
  const { cores, escala, altoContraste } = usePreferencias();
  const tamanho = 30 * escala;
  const estiloTitulo = { fontSize: tamanho, lineHeight: tamanho * 1.15, fontWeight: '800' as const, letterSpacing: -0.8, color: cores.texto };
  return (
    <View
      style={[
        estilos.cabecalho,
        { backgroundColor: cores.cartao },
        altoContraste ? { borderWidth: 2, borderColor: cores.borda } : { boxShadow: SOMBRA },
      ]}
    >
      {/* Enfeite: meia-lua verde-limão no canto, com o ícone da tela. */}
      {icone && (
        <View style={[estilos.enfeite, { backgroundColor: cores.destaque }]} accessible={false} importantForAccessibility="no-hide-descendants">
          <View style={{ position: 'absolute', left: 40, bottom: 34 }}>
            <Icone nome={icone} tamanho={34} cor={cores.sobreDestaque} />
          </View>
        </View>
      )}
      <View accessible accessibilityRole="header" accessibilityLabel={destaque ? `${titulo} ${destaque}` : titulo} style={{ gap: 6, alignItems: 'flex-start' }}>
        <Text style={[estiloTitulo, { paddingRight: icone ? 84 : 0 }]}>{titulo}</Text>
        {destaque && (
          // Etiqueta inteira numa linha só (nunca quebra no meio da palavra), levemente inclinada como adesivo.
          <View style={[estilos.etiqueta, { backgroundColor: cores.destaque, transform: [{ rotate: '-1.5deg' }] }]}>
            <Text style={[estiloTitulo, { color: cores.sobreDestaque }]}>{destaque}</Text>
          </View>
        )}
      </View>
      {subtitulo && <Texto suave style={{ marginTop: 10 }}>{subtitulo}</Texto>}
    </View>
  );
}

export function Texto({ children, tamanho = 18, negrito, suave, papel, cor, style, ...resto }: TextProps & { tamanho?: number; negrito?: boolean; suave?: boolean; papel?: 'header' | 'text'; cor?: string }) {
  const { cores, escala } = usePreferencias();
  const herdada = useContext(CorDoTexto);
  const corFinal = cor ?? (suave ? herdada?.suave ?? cores.suave : herdada?.texto ?? cores.texto);
  return (
    <Text
      accessibilityRole={papel}
      style={[{ fontSize: tamanho * escala, lineHeight: tamanho * escala * 1.35, color: corFinal, fontWeight: negrito ? '800' : '400' }, style]}
      {...resto}
    >
      {children}
    </Text>
  );
}

export function Titulo({ children, acao }: { children: string; acao?: ReactNode }) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 12, flexWrap: 'wrap', gap: 8 }}>
      <Texto tamanho={22} negrito papel="header">{children}</Texto>
      {acao}
    </View>
  );
}

type Variante = 'primario' | 'secundario' | 'destaque';

function coresDoBotao(v: Variante, c: Cores) {
  if (v === 'primario') return { fundo: c.primaria, texto: c.sobrePrimaria, borda: c.primaria };
  if (v === 'destaque') return { fundo: c.destaque, texto: c.sobreDestaque, borda: c.destaque };
  return { fundo: c.cartao, texto: c.texto, borda: c.borda };
}

export function Botao({ titulo, onPress, variante = 'primario', icone, rotulo, dica, compacto, desativado, carregando, selecionado }: {
  titulo: string; onPress: () => void; variante?: Variante; icone?: NomeIcone; rotulo?: string; dica?: string;
  compacto?: boolean; desativado?: boolean; carregando?: boolean; selecionado?: boolean;
}) {
  const { cores, escala, altoContraste } = usePreferencias();
  const c = coresDoBotao(variante, cores);
  const inativo = desativado || carregando;
  return (
    <Pressable
      onPress={inativo ? undefined : onPress}
      accessibilityRole="button"
      accessibilityLabel={rotulo ?? titulo}
      accessibilityHint={dica}
      accessibilityState={{ disabled: inativo, busy: carregando, selected: selecionado }}
      style={({ pressed }) => [
        estilos.botao,
        {
          minHeight: compacto ? 48 : 60,
          paddingHorizontal: compacto ? 18 : 24,
          alignSelf: compacto ? 'flex-start' : 'stretch',
          backgroundColor: c.fundo,
          borderColor: altoContraste ? cores.borda : c.borda,
          opacity: desativado ? 0.45 : 1,
          transform: [{ scale: pressed && !inativo ? 0.97 : 1 }],
        },
      ]}
    >
      {carregando ? <ActivityIndicator color={c.texto} /> : icone && <Icone nome={icone} tamanho={(compacto ? 20 : 24) * Math.min(escala, 1.25)} cor={c.texto} />}
      <Text style={{ fontSize: (compacto ? 17 : 19) * escala, fontWeight: '700', color: c.texto, textAlign: 'center', flexShrink: 1 }}>{titulo}</Text>
    </Pressable>
  );
}

export function BotaoVoltar() {
  const { cores, escala, altoContraste } = usePreferencias();
  return (
    <Pressable
      onPress={() => (router.canGoBack() ? router.back() : router.navigate('/'))}
      accessibilityRole="button"
      accessibilityLabel="Voltar"
      style={({ pressed }) => [estilos.voltar, { backgroundColor: cores.cartao, borderColor: cores.borda, borderWidth: altoContraste ? 2 : 1, opacity: pressed ? 0.7 : 1 }]}
    >
      <Icone nome="voltar" tamanho={22} cor={cores.texto} />
      <Text style={{ fontSize: 17 * escala, fontWeight: '700', color: cores.texto }}>Voltar</Text>
    </Pressable>
  );
}

type Tom = 'normal' | 'escuro' | 'destaque' | 'alerta';

export function Cartao({ children, tom = 'normal', onPress, rotulo, dica, style }: { children: ReactNode; tom?: Tom; onPress?: () => void; rotulo?: string; dica?: string; style?: StyleProp<ViewStyle> }) {
  const { cores, altoContraste } = usePreferencias();
  const fundo = { normal: cores.cartao, escuro: cores.heroi, destaque: cores.destaque, alerta: cores.alertaFundo }[tom];
  const texto = {
    normal: null,
    escuro: { texto: cores.sobreHeroi, suave: cores.heroiSuave },
    destaque: { texto: cores.sobreDestaque, suave: cores.sobreDestaque },
    alerta: { texto: cores.texto, suave: cores.texto },
  }[tom];
  const estilo = [
    estilos.cartao,
    { backgroundColor: fundo },
    altoContraste ? { borderWidth: 2, borderColor: tom === 'normal' ? cores.borda : cores.primaria } : tom === 'normal' ? { boxShadow: SOMBRA } : null,
    tom === 'alerta' && !altoContraste ? { borderWidth: 2, borderColor: cores.alerta } : null,
    style,
  ];
  const conteudo = <CorDoTexto.Provider value={texto}>{children}</CorDoTexto.Provider>;
  if (!onPress) return <View style={estilo} accessible={!!rotulo} accessibilityLabel={rotulo}>{conteudo}</View>;
  return (
    <Pressable style={({ pressed }) => [estilo, { transform: [{ scale: pressed ? 0.98 : 1 }] }]} onPress={onPress} accessibilityRole="button" accessibilityLabel={rotulo} accessibilityHint={dica}>
      {conteudo}
    </Pressable>
  );
}

export type TomSelo = 'alerta' | 'aviso' | 'sucesso' | 'neutro' | 'destaque';

/** Selo de situação ("vence amanhã", "Concluída"): pílula colorida com texto, nunca só cor. */
export function Selo({ texto, tom = 'neutro', icone }: { texto: string; tom?: TomSelo; icone?: NomeIcone }) {
  const { cores, escala, altoContraste } = usePreferencias();
  const c = {
    alerta: [cores.alertaFundo, cores.alerta],
    aviso: [cores.avisoFundo, cores.aviso],
    sucesso: [cores.sucessoFundo, cores.sucesso],
    neutro: [cores.campo, cores.texto],
    destaque: [cores.destaque, cores.sobreDestaque],
  }[tom] as [string, string];
  return (
    <View style={[estilos.selo, { backgroundColor: c[0] }, (altoContraste || tom === 'neutro') && { borderWidth: 1.5, borderColor: altoContraste ? c[1] : cores.borda }]}>
      {icone && <Icone nome={icone} tamanho={16 * Math.min(escala, 1.25)} cor={c[1]} />}
      <Text style={{ color: c[1], fontSize: 15 * escala, fontWeight: '700' }}>{texto}</Text>
    </View>
  );
}

/** Ícone dentro de um círculo colorido (como nos atalhos e nos cartões). */
export function CirculoIcone({ nome, tamanho = 56, fundo, cor }: { nome: NomeIcone; tamanho?: number; fundo?: string; cor?: string }) {
  const { cores, altoContraste } = usePreferencias();
  return (
    <View
      style={{
        width: tamanho, height: tamanho, borderRadius: tamanho / 2, alignItems: 'center', justifyContent: 'center',
        backgroundColor: fundo ?? cores.campo, borderWidth: altoContraste ? 1.5 : 0, borderColor: cores.borda,
      }}
    >
      <Icone nome={nome} tamanho={tamanho * 0.48} cor={cor ?? cores.texto} />
    </View>
  );
}

/** Escolha de uma opção (rádio grande): cartão inteiro clicável, marcado em verde-limão. */
export function Opcao({ titulo, icone, escolhida, onPress, descricao }: { titulo: string; icone?: NomeIcone; escolhida: boolean; onPress: () => void; descricao?: string }) {
  const { cores, altoContraste } = usePreferencias();
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="radio"
      accessibilityState={{ checked: escolhida }}
      accessibilityLabel={titulo}
      style={({ pressed }) => [
        estilos.opcao,
        {
          backgroundColor: escolhida ? cores.destaque : cores.cartao,
          borderColor: escolhida ? (altoContraste ? cores.borda : cores.primaria) : altoContraste ? cores.borda : cores.cartao,
          transform: [{ scale: pressed ? 0.98 : 1 }],
        },
        !altoContraste && !escolhida && { boxShadow: SOMBRA },
      ]}
    >
      <CorDoTexto.Provider value={escolhida ? { texto: cores.sobreDestaque, suave: cores.sobreDestaque } : null}>
        {icone && <CirculoIcone nome={icone} tamanho={48} fundo={escolhida ? cores.cartao : cores.campo} cor={cores.texto} />}
        <View style={{ flex: 1, gap: 2 }}>
          <Texto tamanho={19} negrito>{titulo}</Texto>
          {descricao && <Texto suave tamanho={15}>{descricao}</Texto>}
        </View>
        <View style={[estilos.radio, { borderColor: escolhida ? cores.primaria : cores.suave, backgroundColor: escolhida ? cores.primaria : 'transparent' }]}>
          {escolhida && <Icone nome="certo" tamanho={18} cor={cores.sobrePrimaria} />}
        </View>
      </CorDoTexto.Provider>
    </Pressable>
  );
}

/** Número do passo ("1", "2", "3") com título: ajuda a seguir a ordem. */
export function Passo({ numero, titulo }: { numero: number; titulo: string }) {
  const { cores, escala } = usePreferencias();
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, marginTop: 12 }} accessible accessibilityRole="header" accessibilityLabel={`Passo ${numero}: ${titulo}`}>
      <View style={{ width: 36 * escala, height: 36 * escala, borderRadius: 18 * escala, backgroundColor: cores.primaria, alignItems: 'center', justifyContent: 'center' }}>
        <Text style={{ color: cores.sobrePrimaria, fontWeight: '800', fontSize: 17 * escala }}>{numero}</Text>
      </View>
      <Texto tamanho={22} negrito style={{ flex: 1 }}>{titulo}</Texto>
    </View>
  );
}

/** Campo de texto grande, com rótulo sempre visível em cima. */
export function Campo({ rotulo, valor, mudar, dica }: { rotulo: string; valor: string; mudar: (v: string) => void; dica?: string }) {
  const { cores, escala, altoContraste } = usePreferencias();
  return (
    <View style={{ gap: 6 }}>
      <Texto tamanho={16} negrito>{rotulo}</Texto>
      <TextInput
        value={valor}
        onChangeText={mudar}
        placeholder={dica}
        placeholderTextColor={cores.suave}
        accessibilityLabel={rotulo}
        autoCorrect={false}
        style={{
          minHeight: 60, borderRadius: 20, paddingHorizontal: 18, fontSize: 19 * escala, color: cores.texto,
          backgroundColor: cores.campo, borderWidth: 2, borderColor: altoContraste ? cores.borda : cores.campo,
        }}
      />
    </View>
  );
}

export function Carregando({ erro, vazio, carregando }: { erro?: unknown; vazio?: boolean; carregando?: boolean }) {
  const { cores } = usePreferencias();
  if (carregando)
    return (
      <Cartao rotulo="Carregando">
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
          <ActivityIndicator size="large" color={cores.texto} />
          <Texto>Buscando suas informações…</Texto>
        </View>
      </Cartao>
    );
  if (erro)
    return (
      <Cartao tom="alerta">
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
          <Icone nome="semInternet" tamanho={28} cor={cores.alerta} />
          <Texto style={{ flex: 1 }}>Não consegui buscar agora. Confira a internet e tente de novo.</Texto>
        </View>
      </Cartao>
    );
  if (vazio)
    return (
      <Cartao>
        <View style={{ alignItems: 'center', gap: 10, paddingVertical: 12 }}>
          <CirculoIcone nome="vazio" tamanho={64} />
          <Texto suave>Nada por aqui ainda.</Texto>
        </View>
      </Cartao>
    );
  return null;
}

const estilos = StyleSheet.create({
  tela: { padding: 20, paddingTop: 12, paddingBottom: 32, gap: 16 },
  cabecalho: { borderRadius: 32, padding: 24, paddingTop: 26, overflow: 'hidden', marginBottom: 4 },
  etiqueta: { borderRadius: 16, paddingHorizontal: 14, paddingBottom: 4, paddingTop: 2, marginLeft: -4 },
  enfeite: { position: 'absolute', top: -46, right: -46, width: 150, height: 150, borderRadius: 75 },
  botao: { borderRadius: 999, borderWidth: 2, paddingVertical: 12, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 10 },
  voltar: { alignSelf: 'flex-start', flexDirection: 'row', alignItems: 'center', gap: 6, minHeight: 52, paddingLeft: 14, paddingRight: 20, borderRadius: 999 },
  cartao: { borderRadius: 28, padding: 20, gap: 12 },
  selo: { alignSelf: 'flex-start', flexDirection: 'row', alignItems: 'center', gap: 6, borderRadius: 999, paddingHorizontal: 12, paddingVertical: 6 },
  opcao: { flexDirection: 'row', alignItems: 'center', gap: 14, minHeight: 72, padding: 14, borderRadius: 24, borderWidth: 2 },
  radio: { width: 30, height: 30, borderRadius: 15, borderWidth: 2, alignItems: 'center', justifyContent: 'center' },
});
