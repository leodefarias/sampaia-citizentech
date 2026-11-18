import { defineConfig } from 'wxt';
import { PADROES_HOST } from './lib/config';

export default defineConfig({
  modules: ['@wxt-dev/module-react'],
  manifest: {
    name: 'SampaIA — Paulinha',
    description: 'A Paulinha resolve serviços públicos de São Paulo junto com você.',
    action: { default_title: 'Abrir a Paulinha' },
    permissions: ['sidePanel', 'storage', 'tabs'],
    // Só estes sites. FORA DE PROPÓSITO: *.acesso.gov.br (login gov.br) e páginas de pagamento.
    // O navegador impede a extensão de ler qualquer coisa fora desta lista.
    host_permissions: PADROES_HOST,
  },
});
