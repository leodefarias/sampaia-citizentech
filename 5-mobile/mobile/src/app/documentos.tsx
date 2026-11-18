import { useQuery } from '@tanstack/react-query';
import * as Clipboard from 'expo-clipboard';
import { useState } from 'react';
import { View } from 'react-native';
import { Botao, Cartao, Carregando, CirculoIcone, Icone, Selo, Tela, Texto, type NomeIcone, type TomSelo } from '@/components/ui';
import { api } from '@/lib/api';
import { comMaiuscula, tomDoPrazo } from '@/lib/aparencia';
import { diaFalado, quandoVence, reais } from '@/lib/datas';
import { usePreferencias } from '@/lib/preferencias';
import type { Documento } from '@/lib/tipos';

const NOMES: Record<Documento['tipo'], string> = { boleto: 'Boleto', pix: 'Pix', protocolo: 'Protocolo', agendamento: 'Agendamento' };
const ICONES: Record<Documento['tipo'], NomeIcone> = { boleto: 'boleto', pix: 'pix', protocolo: 'protocolo', agendamento: 'calendario' };

function frase(d: Documento) {
  const partes = [d.titulo ? `${NOMES[d.tipo]}: ${d.titulo}` : NOMES[d.tipo]];
  if (d.valor != null) partes.push(`de ${reais(d.valor)}`);
  if (d.vencimento) partes.push(d.tipo === 'agendamento' ? `marcado para ${diaFalado(d.vencimento)}` : quandoVence(d.vencimento));
  return partes.join(' ');
}

function situacao(d: Documento): { texto: string; tom: TomSelo } {
  if (d.tipo === 'agendamento' && d.vencimento) return { texto: `Marcado para ${diaFalado(d.vencimento)}`, tom: 'sucesso' };
  if (d.vencimento) return { texto: comMaiuscula(quandoVence(d.vencimento)), tom: tomDoPrazo(d.vencimento) };
  return { texto: 'Pedido aberto', tom: 'neutro' };
}

export default function Documentos() {
  const { cores, altoContraste, falar, lerEmVoz } = usePreferencias();
  const [copiado, setCopiado] = useState<number>();
  const { data, error, isLoading } = useQuery({ queryKey: ['documentos'], queryFn: api.documentos });

  return (
    <Tela titulo="Seus" destaque="documentos" icone="documentos" subtitulo="Boletos, agendamentos e protocolos num só lugar, prontos quando você precisar.">
      <Carregando carregando={isLoading} erro={error} vazio={data?.length === 0} />
      {data?.map((d) => {
        const s = situacao(d);
        return (
          <Cartao key={d.id} rotulo={frase(d)}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
              <CirculoIcone nome={ICONES[d.tipo]} />
              <View style={{ flex: 1, gap: 6 }}>
                <Texto tamanho={22} negrito>{d.titulo ?? NOMES[d.tipo]}</Texto>
                <Selo tom={s.tom} texto={s.texto} />
              </View>
            </View>

            {d.valor != null && <Texto tamanho={32} negrito style={{ letterSpacing: -0.5 }}>{reais(d.valor)}</Texto>}

            {d.codigo && (
              <View style={{ backgroundColor: cores.campo, borderRadius: 18, padding: 14, gap: 2, borderWidth: altoContraste ? 1.5 : 0, borderColor: cores.borda }}>
                <Texto tamanho={15} suave>Número</Texto>
                <Texto tamanho={20} negrito selectable>{d.codigo}</Texto>
              </View>
            )}

            {(d.tipo === 'boleto' || d.tipo === 'pix') && (
              <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}>
                <Icone nome="cadeado" tamanho={20} cor={cores.suave} />
                <Texto suave tamanho={16} style={{ flex: 1 }}>
                  Por segurança, o código para pagar fica só no computador onde a Paulinha gerou o boleto.
                </Texto>
              </View>
            )}

            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 10 }}>
              {d.codigo && (
                <Botao
                  compacto
                  icone={copiado === d.id ? 'certo' : 'copiar'}
                  titulo={copiado === d.id ? 'Copiado!' : 'Copiar número'}
                  onPress={async () => {
                    await Clipboard.setStringAsync(d.codigo!);
                    setCopiado(d.id);
                  }}
                />
              )}
              {lerEmVoz && <Botao compacto variante="secundario" icone="ouvir" titulo="Ouvir" rotulo={`Ouvir: ${frase(d)}`} onPress={() => falar(frase(d))} />}
            </View>
          </Cartao>
        );
      })}
    </Tela>
  );
}
