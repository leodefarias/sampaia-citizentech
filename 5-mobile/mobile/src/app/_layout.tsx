import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { View } from 'react-native';
import { BarraInferior } from '@/components/BarraInferior';
import { PreferenciasProvider, usePreferencias } from '@/lib/preferencias';

const cliente = new QueryClient({ defaultOptions: { queries: { staleTime: 60_000, retry: 1 } } });

function Navegacao() {
  const { cores, altoContraste } = usePreferencias();
  return (
    <View style={{ flex: 1, backgroundColor: cores.fundo }}>
      <StatusBar style={altoContraste ? 'light' : 'dark'} />
      {/* Sem cabeçalho nativo: cada tela mostra título grande e, quando precisa, um botão "Voltar" com texto. */}
      <Stack screenOptions={{ headerShown: false, animation: 'fade', contentStyle: { backgroundColor: cores.fundo } }}>
        <Stack.Screen name="index" options={{ title: 'Início · SampaIA' }} />
        <Stack.Screen name="tarefas" options={{ title: 'O que já resolvemos · SampaIA' }} />
        <Stack.Screen name="documentos" options={{ title: 'Seus documentos · SampaIA' }} />
        <Stack.Screen name="pedido" options={{ title: 'Pedir conserto · SampaIA' }} />
        <Stack.Screen name="seguranca" options={{ title: 'Seus dados protegidos · SampaIA' }} />
        <Stack.Screen name="ajustes" options={{ title: 'Ajustes · SampaIA' }} />
      </Stack>
      <BarraInferior />
    </View>
  );
}

export default function Layout() {
  return (
    <QueryClientProvider client={cliente}>
      <PreferenciasProvider>
        <Navegacao />
      </PreferenciasProvider>
    </QueryClientProvider>
  );
}
