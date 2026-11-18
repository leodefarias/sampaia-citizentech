// Descobre o distrito de São Paulo a partir da posição, TUDO no aparelho.
// Não usa geocodificação reversa: na web ela não existe mais no expo-location (sempre volta vazia)
// e no Android ela manda a coordenada para um serviço externo. Aqui a posição exata nunca sai do celular.
import { CENTROS_DOS_DISTRITOS } from './distritos.ts';

/** Mais longe que isso do centro de qualquer distrito: a pessoa provavelmente não está na capital. */
const DISTANCIA_MAXIMA_KM = 8;

function distanciaKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  // Aproximação plana: suficiente para distâncias dentro de uma cidade.
  const kmPorGrauLat = 111.32;
  const kmPorGrauLon = kmPorGrauLat * Math.cos(((lat1 + lat2) / 2) * (Math.PI / 180));
  return Math.hypot((lat1 - lat2) * kmPorGrauLat, (lon1 - lon2) * kmPorGrauLon);
}

/** Nome do distrito como o modelo de ML conhece (ex.: "VILA MARIANA"), ou null fora da capital. */
export function distritoMaisProximo(lat: number, lon: number): string | null {
  let melhor: string | null = null;
  let menor = Infinity;
  for (const [nome, dLat, dLon] of CENTROS_DOS_DISTRITOS) {
    const d = distanciaKm(lat, lon, dLat, dLon);
    if (d < menor) {
      menor = d;
      melhor = nome;
    }
  }
  return menor <= DISTANCIA_MAXIMA_KM ? melhor : null;
}

const MINUSCULAS = new Set(['DA', 'DE', 'DO', 'DAS', 'DOS']);

// A base do SP156 guarda os nomes sem acento; para a tela, devolvemos a grafia oficial.
const ACENTOS: Record<string, string> = {
  AGUA: 'Água', ANGELA: 'Ângela', BELEM: 'Belém', BONIFACIO: 'Bonifácio', BRAS: 'Brás', BRASILANDIA: 'Brasilândia',
  BUTANTA: 'Butantã', CANGAIBA: 'Cangaíba', CAPAO: 'Capão', CARRAO: 'Carrão', CECILIA: 'Cecília', CONSOLACAO: 'Consolação',
  CURUCA: 'Curuçá', GRAJAU: 'Grajaú', JACANA: 'Jaçanã', JACUI: 'Jacuí', JAGUARE: 'Jaguaré', JARAGUA: 'Jaraguá', JOSE: 'José',
  LIDER: 'Líder', LIMAO: 'Limão', LUIS: 'Luís', O: 'Ó', REPUBLICA: 'República', SACOMA: 'Sacomã', SAO: 'São', SAUDE: 'Saúde',
  SE: 'Sé', SONIA: 'Sônia', TATUAPE: 'Tatuapé', TREMEMBE: 'Tremembé',
};

/** "FREGUESIA DO O" -> "Freguesia do Ó", "SE" -> "Sé": só para mostrar na tela. */
export function nomeDoDistrito(nome: string): string {
  return nome
    .split(' ')
    .map((p, i) => {
      if (i > 0 && MINUSCULAS.has(p)) return p.toLowerCase();
      return ACENTOS[p] ?? p.charAt(0) + p.slice(1).toLowerCase();
    })
    .join(' ');
}
