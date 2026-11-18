// Ícones do sistema: SF Symbols no iOS e Material Symbols no Android e na web (fonte local, sem internet).
// Ícone é sempre decorativo: o texto ao lado diz o que é.
import { SymbolView, type SymbolViewProps } from 'expo-symbols';

const ICONES = {
  inicio: { ios: 'house.fill', android: 'home', web: 'home' },
  documentos: { ios: 'doc.text.fill', android: 'description', web: 'description' },
  pedido: { ios: 'wrench.and.screwdriver.fill', android: 'construction', web: 'construction' },
  ajustes: { ios: 'gearshape.fill', android: 'settings', web: 'settings' },
  tarefas: { ios: 'clock.arrow.circlepath', android: 'history', web: 'history' },
  seguranca: { ios: 'checkmark.shield.fill', android: 'verified_user', web: 'verified_user' },
  ouvir: { ios: 'speaker.wave.2.fill', android: 'volume_up', web: 'volume_up' },
  voltar: { ios: 'chevron.left', android: 'arrow_back', web: 'arrow_back' },
  seguir: { ios: 'chevron.right', android: 'chevron_right', web: 'chevron_right' },
  copiar: { ios: 'doc.on.doc.fill', android: 'content_copy', web: 'content_copy' },
  camera: { ios: 'camera.fill', android: 'photo_camera', web: 'photo_camera' },
  local: { ios: 'location.fill', android: 'my_location', web: 'my_location' },
  certo: { ios: 'checkmark', android: 'check', web: 'check' },
  fechar: { ios: 'xmark', android: 'close', web: 'close' },
  agua: { ios: 'drop.fill', android: 'water_drop', web: 'water_drop' },
  casa: { ios: 'house.lodge.fill', android: 'home_work', web: 'home_work' },
  carro: { ios: 'car.fill', android: 'directions_car', web: 'directions_car' },
  calendario: { ios: 'calendar', android: 'calendar_month', web: 'calendar_month' },
  boleto: { ios: 'barcode', android: 'barcode', web: 'barcode' },
  pix: { ios: 'qrcode', android: 'qr_code_2', web: 'qr_code_2' },
  protocolo: { ios: 'number', android: 'tag', web: 'tag' },
  cadeado: { ios: 'lock.fill', android: 'lock', web: 'lock' },
  buraco: { ios: 'exclamationmark.triangle.fill', android: 'warning', web: 'warning' },
  entulho: { ios: 'trash.fill', android: 'delete', web: 'delete' },
  arvore: { ios: 'tree.fill', android: 'park', web: 'park' },
  bueiro: { ios: 'drop.triangle.fill', android: 'water_damage', web: 'water_damage' },
  varricao: { ios: 'leaf.fill', android: 'cleaning_services', web: 'cleaning_services' },
  relogio: { ios: 'clock.fill', android: 'schedule', web: 'schedule' },
  computador: { ios: 'desktopcomputer', android: 'computer', web: 'computer' },
  letra: { ios: 'textformat.size', android: 'format_size', web: 'format_size' },
  contraste: { ios: 'circle.lefthalf.filled', android: 'contrast', web: 'contrast' },
  paulinha: { ios: 'face.smiling.inverse', android: 'sentiment_satisfied', web: 'sentiment_satisfied' },
  senha: { ios: 'key.fill', android: 'key', web: 'key' },
  maoLevantada: { ios: 'hand.raised.fill', android: 'back_hand', web: 'back_hand' },
  semInternet: { ios: 'wifi.slash', android: 'wifi_off', web: 'wifi_off' },
  vazio: { ios: 'tray', android: 'inbox', web: 'inbox' },
  aviso: { ios: 'bell.fill', android: 'notifications', web: 'notifications' },
  telefone: { ios: 'phone.fill', android: 'call', web: 'call' },
  abrir: { ios: 'arrow.up.right.square.fill', android: 'open_in_new', web: 'open_in_new' },
} satisfies Record<string, SymbolViewProps['name']>;

export type NomeIcone = keyof typeof ICONES;

export function Icone({ nome, tamanho = 24, cor }: { nome: NomeIcone; tamanho?: number; cor: string }) {
  return (
    <SymbolView
      name={ICONES[nome]}
      size={tamanho}
      tintColor={cor}
      accessible={false}
      importantForAccessibility="no-hide-descendants"
      fallback={null}
    />
  );
}
