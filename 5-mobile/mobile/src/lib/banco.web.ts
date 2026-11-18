// Versão web (só para demo e prints): localStorage no lugar do expo-sqlite. No celular vale banco.ts.
export function guardar(chave: string, valor: unknown) {
  localStorage.setItem('cache:' + chave, JSON.stringify(valor));
}

export function lerGuardado<T>(chave: string): T | undefined {
  const v = localStorage.getItem('cache:' + chave);
  return v ? (JSON.parse(v) as T) : undefined;
}

export const preferenciasGuardadas = {
  ler: (chave: string) => (typeof localStorage === 'undefined' ? null : localStorage.getItem(chave)),
  gravar: (chave: string, valor: string) => localStorage.setItem(chave, valor),
};
