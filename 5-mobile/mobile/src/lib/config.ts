// Endereços das APIs. No celular, "localhost" é o próprio celular: use o IP do computador (ex.: http://192.168.0.10:5009).
export const API_AGENTE = process.env.EXPO_PUBLIC_API_AGENTE ?? '';
export const API_LEMBRETES = process.env.EXPO_PUBLIC_API_LEMBRETES ?? '';

/** Sem endereço configurado, o app roda com dados simulados (JSON local). */
export const MODO_MOCK = process.env.EXPO_PUBLIC_MOCK === '1' || !API_AGENTE || !API_LEMBRETES;
