// Configuração do Node-RED da SampaIA (copiada para /opt/sampaia/node-red/ pelo 03_nodered.sh).
// O editor fica só em localhost: para abrir de fora, use um túnel SSH
//   ssh -L 1880:localhost:1880 usuario@ip-da-vm   e acesse http://localhost:1880
module.exports = {
  uiHost: '127.0.0.1',
  uiPort: 1880,
  flowFile: 'flows.json',
  // Sem credenciais guardadas no fluxo (broker sem senha); evita o aviso de chave gerada.
  credentialSecret: false,
  logging: { console: { level: 'info', metrics: false, audit: false } },
};
