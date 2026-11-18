// Banco local (expo-sqlite): guarda a última resposta de cada lista para o app funcionar sem internet.
import * as SQLite from 'expo-sqlite';
import Storage from 'expo-sqlite/kv-store';

const db = SQLite.openDatabaseSync('sampaia.db');
db.execSync('CREATE TABLE IF NOT EXISTS cache (chave TEXT PRIMARY KEY NOT NULL, json TEXT NOT NULL, em TEXT NOT NULL)');

export function guardar(chave: string, valor: unknown) {
  db.runSync('INSERT OR REPLACE INTO cache (chave, json, em) VALUES (?, ?, ?)', chave, JSON.stringify(valor), new Date().toISOString());
}

export function lerGuardado<T>(chave: string): T | undefined {
  const linha = db.getFirstSync<{ json: string }>('SELECT json FROM cache WHERE chave = ?', chave);
  return linha ? (JSON.parse(linha.json) as T) : undefined;
}

/** Preferências simples (tamanho da letra, contraste, voz). */
export const preferenciasGuardadas = {
  ler: (chave: string) => Storage.getItemSync(chave),
  gravar: (chave: string, valor: string) => Storage.setItemSync(chave, valor),
};
