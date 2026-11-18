// Barra inferior flutuante. Ícone + nome sempre visíveis (nada de ícone sozinho) e alvos de 64 dp.
import { router, usePathname, type Href } from 'expo-router';
import { Pressable, Text, useWindowDimensions, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { usePreferencias } from '@/lib/preferencias';
import { Icone, type NomeIcone } from './Icone';

const ABAS: { rota: Href & string; nome: string; icone: NomeIcone }[] = [
  { rota: '/', nome: 'Início', icone: 'inicio' },
  { rota: '/documentos', nome: 'Documentos', icone: 'documentos' },
  { rota: '/pedido', nome: 'Pedir 156', icone: 'pedido' },
  { rota: '/ajustes', nome: 'Ajustes', icone: 'ajustes' },
];

export function BarraInferior() {
  const caminho = usePathname();
  const { cores, altoContraste } = usePreferencias();
  const { bottom } = useSafeAreaInsets();
  const { width } = useWindowDimensions();
  // A fonte da barra não acompanha a escala: "Documentos" precisa caber inteiro mesmo em telas de 360 dp.
  const fonte = width < 380 ? 13 : 14;

  return (
    <View style={{ backgroundColor: cores.fundo, paddingHorizontal: 8, paddingTop: 6, paddingBottom: Math.max(bottom, 12) }}>
      <View
        accessibilityRole="tablist"
        style={{
          flexDirection: 'row', gap: 0, padding: 4, borderRadius: 32, backgroundColor: cores.cartao,
          ...(altoContraste ? { borderWidth: 2, borderColor: cores.borda } : { boxShadow: '0px 8px 28px rgba(21, 23, 28, 0.12)' }),
        }}
      >
        {ABAS.map((a) => {
          const ativa = caminho === a.rota;
          const cor = ativa ? cores.sobrePrimaria : cores.suave;
          return (
            <Pressable
              key={a.rota}
              accessibilityRole="tab"
              accessibilityState={{ selected: ativa }}
              accessibilityLabel={a.nome}
              onPress={() => !ativa && router.navigate(a.rota)}
              style={({ pressed }) => ({
                flex: 1, minHeight: 64, borderRadius: 26, alignItems: 'center', justifyContent: 'center', gap: 2,
                backgroundColor: ativa ? cores.primaria : pressed ? cores.campo : 'transparent',
              })}
            >
              <Icone nome={a.icone} tamanho={26} cor={cor} />
              <Text numberOfLines={1} adjustsFontSizeToFit style={{ fontSize: fonte, fontWeight: ativa ? '800' : '600', color: cor, letterSpacing: -0.3 }}>
                {a.nome}
              </Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}
