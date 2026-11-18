import * as Speech from 'expo-speech';
import { createContext, useContext, useState, type ReactNode } from 'react';
import { preferenciasGuardadas } from './banco';

export type TamanhoLetra = 'normal' | 'grande' | 'enorme';
const ESCALA: Record<TamanhoLetra, number> = { normal: 1, grande: 1.25, enorme: 1.5 };

// Paleta "SampaIA": fundo cinza-claro, cartões brancos, botões escuros e destaque verde-limão.
// Contraste mínimo 4,5:1 em todo texto (alto contraste chega a 21:1).
const CORES = {
  normal: {
    fundo: '#F1F2EE', cartao: '#FFFFFF', campo: '#F1F2EE', borda: '#E2E4DE',
    texto: '#15171C', suave: '#555A63',
    primaria: '#15171C', sobrePrimaria: '#FFFFFF',
    heroi: '#15171C', sobreHeroi: '#FFFFFF', heroiSuave: '#C9CCD2',
    destaque: '#C6F16B', sobreDestaque: '#15171C',
    alerta: '#B4380F', alertaFundo: '#FFE9DF',
    aviso: '#8A5A00', avisoFundo: '#FFF3D1',
    sucesso: '#1F7A3A', sucessoFundo: '#E2F5E6',
  },
  contraste: {
    fundo: '#000000', cartao: '#000000', campo: '#000000', borda: '#FFFFFF',
    texto: '#FFFFFF', suave: '#FFFFFF',
    primaria: '#FFD400', sobrePrimaria: '#000000',
    heroi: '#000000', sobreHeroi: '#FFFFFF', heroiSuave: '#FFFFFF',
    destaque: '#FFD400', sobreDestaque: '#000000',
    alerta: '#FFD400', alertaFundo: '#000000',
    aviso: '#FFD400', avisoFundo: '#000000',
    sucesso: '#9BFFB0', sucessoFundo: '#000000',
  },
};

export type Cores = (typeof CORES)['normal'];

interface Preferencias {
  tamanho: TamanhoLetra;
  altoContraste: boolean;
  lerEmVoz: boolean;
  escala: number;
  cores: Cores;
  mudar: (p: Partial<Pick<Preferencias, 'tamanho' | 'altoContraste' | 'lerEmVoz'>>) => void;
  falar: (texto: string) => void;
}

const Contexto = createContext<Preferencias | null>(null);

export function PreferenciasProvider({ children }: { children: ReactNode }) {
  const [tamanho, setTamanho] = useState<TamanhoLetra>(() => (preferenciasGuardadas.ler('tamanho') as TamanhoLetra) ?? 'grande');
  const [altoContraste, setContraste] = useState(() => preferenciasGuardadas.ler('contraste') === '1');
  const [lerEmVoz, setVoz] = useState(() => preferenciasGuardadas.ler('voz') !== '0');

  const valor: Preferencias = {
    tamanho, altoContraste, lerEmVoz,
    escala: ESCALA[tamanho],
    cores: altoContraste ? CORES.contraste : CORES.normal,
    mudar: (p) => {
      if (p.tamanho) { setTamanho(p.tamanho); preferenciasGuardadas.gravar('tamanho', p.tamanho); }
      if (p.altoContraste !== undefined) { setContraste(p.altoContraste); preferenciasGuardadas.gravar('contraste', p.altoContraste ? '1' : '0'); }
      if (p.lerEmVoz !== undefined) { setVoz(p.lerEmVoz); preferenciasGuardadas.gravar('voz', p.lerEmVoz ? '1' : '0'); }
    },
    falar: (texto) => {
      Speech.stop();
      Speech.speak(texto, { language: 'pt-BR', rate: 0.9 });
    },
  };
  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}

export function usePreferencias() {
  const p = useContext(Contexto);
  if (!p) throw new Error('usePreferencias fora do PreferenciasProvider');
  return p;
}
