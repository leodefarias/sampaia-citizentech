import { useMutation, useQueryClient } from '@tanstack/react-query';
import * as Clipboard from 'expo-clipboard';
import { CameraView, useCameraPermissions } from 'expo-camera';
import { Image } from 'expo-image';
import * as Linking from 'expo-linking';
import * as Location from 'expo-location';
import { router } from 'expo-router';
import * as WebBrowser from 'expo-web-browser';
import { useRef, useState } from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Botao, Campo, Cartao, Icone, Opcao, Passo, Selo, Tela, Texto, type NomeIcone } from '@/components/ui';
import { api } from '@/lib/api';
import { distritoMaisProximo, nomeDoDistrito } from '@/lib/localizacao';
import { usePreferencias } from '@/lib/preferencias';
import { guardarProtocolo } from '@/lib/protocolos';
import { descricaoPronta, ENVIO_156 } from '@/lib/sp156';

// Tipos de zeladoria do SP156 usados no modelo de ML (ver ml/dados/README.md e ROTULOS_APP em ml/preparar.py).
const TIPOS: { nome: string; icone: NomeIcone }[] = [
  { nome: 'Buraco na rua', icone: 'buraco' },
  { nome: 'Entulho ou lixo na rua', icone: 'entulho' },
  { nome: 'Árvore (poda ou queda)', icone: 'arvore' },
  { nome: 'Bueiro entupido', icone: 'bueiro' },
  { nome: 'Falta de varrição', icone: 'varricao' },
];

export default function Pedido() {
  const { cores, falar, lerEmVoz } = usePreferencias();
  const [permissao, pedirPermissao] = useCameraPermissions();
  const camera = useRef<CameraView>(null);
  const [abrirCamera, setAbrirCamera] = useState(false);
  const [foto, setFoto] = useState<string>();
  const [tipo, setTipo] = useState<string>();
  // Nome do distrito como o modelo de ML conhece (ex.: "VILA MARIANA").
  const [distrito, setDistrito] = useState<string | null>();
  const [avisoLocal, setAvisoLocal] = useState<string>();
  const [buscandoLocal, setBuscandoLocal] = useState(false);

  const previsao = useMutation({ mutationFn: api.preverPrazo });

  async function tirarFoto() {
    const f = await camera.current?.takePictureAsync({ quality: 0.5 });
    if (f) setFoto(f.uri);
    setAbrirCamera(false);
  }

  async function pegarLocal() {
    setBuscandoLocal(true);
    setAvisoLocal(undefined);
    try {
      const { granted } = await Location.requestForegroundPermissionsAsync();
      if (!granted) {
        setDistrito(null);
        return setAvisoLocal('O celular não deixou ver onde você está. Tudo bem, dá para seguir sem isso.');
      }
      // Precisão "equilibrada" basta para achar o bairro e responde mais rápido dentro de casa.
      const pos =
        (await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced }).catch(() => null)) ??
        (await Location.getLastKnownPositionAsync().catch(() => null));
      if (!pos) {
        setDistrito(null);
        return setAvisoLocal('Não consegui achar o sinal agora. Tente de novo perto de uma janela, ou siga sem isso.');
      }
      // O distrito é calculado aqui mesmo: a posição exata não sai do celular.
      const achado = distritoMaisProximo(pos.coords.latitude, pos.coords.longitude);
      setDistrito(achado);
      if (!achado) setAvisoLocal('Parece que você não está na cidade de São Paulo. Tudo bem, dá para seguir.');
    } finally {
      setBuscandoLocal(false);
    }
  }

  if (abrirCamera) {
    return (
      <View style={{ flex: 1, backgroundColor: '#000' }}>
        <CameraView ref={camera} style={{ flex: 1 }} facing="back" />
        <SafeAreaView edges={[]} style={{ padding: 16, gap: 10, backgroundColor: '#000' }}>
          <Botao variante="destaque" icone="camera" titulo="Tirar foto" onPress={tirarFoto} />
          <Botao variante="secundario" icone="fechar" titulo="Cancelar" onPress={() => setAbrirCamera(false)} />
        </SafeAreaView>
      </View>
    );
  }

  const frasePrazo = previsao.data && `Pedidos como o seu costumam levar ${previsao.data.faixa}`;

  return (
    <Tela titulo="Pedir" destaque="conserto" icone="pedido" subtitulo="Viu um buraco, lixo ou árvore caída? Descubra em quanto tempo a Prefeitura (156) costuma resolver.">
      <Passo numero={1} titulo="O que você viu?" />
      <View accessibilityRole="radiogroup" style={{ gap: 10 }}>
        {TIPOS.map((t) => (
          <Opcao key={t.nome} titulo={t.nome} icone={t.icone} escolhida={tipo === t.nome} onPress={() => setTipo(t.nome)} />
        ))}
      </View>

      <Passo numero={2} titulo="Tem foto? (se quiser)" />
      {foto && <Image source={{ uri: foto }} style={{ height: 220, borderRadius: 24 }} accessibilityLabel="Foto do problema" />}
      <Botao
        variante="secundario"
        icone="camera"
        titulo={foto ? 'Tirar outra foto' : 'Tirar foto'}
        onPress={async () => {
          if (!permissao?.granted && !(await pedirPermissao()).granted) return;
          setAbrirCamera(true);
        }}
      />

      <Passo numero={3} titulo="Onde fica?" />
      {distrito && <Selo tom="sucesso" icone="local" texto={`Perto de: ${nomeDoDistrito(distrito)}`} />}
      {avisoLocal && <Texto suave>{avisoLocal}</Texto>}
      <Botao variante="secundario" icone="local" titulo={distrito ? 'Procurar de novo' : 'Usar onde estou agora'} carregando={buscandoLocal} onPress={pegarLocal} />
      <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}>
        <Icone nome="cadeado" tamanho={18} cor={cores.suave} />
        <Texto suave tamanho={15} style={{ flex: 1 }}>Só o nome do bairro sai do celular. O endereço exato fica aqui.</Texto>
      </View>

      <View style={{ height: 8 }} />
      <Botao
        icone="relogio"
        titulo={previsao.isPending ? 'Calculando…' : 'Ver quanto tempo leva'}
        carregando={previsao.isPending}
        desativado={!tipo}
        onPress={() => tipo && previsao.mutate({ tipoServico: tipo, distrito: distrito ?? null })}
        dica={tipo ? undefined : 'Escolha primeiro o tipo do problema'}
      />
      {!tipo && <Texto suave tamanho={16} style={{ textAlign: 'center' }}>Primeiro, escolha no passo 1 o que você viu.</Texto>}

      {previsao.data && frasePrazo && (
        <Cartao tom="escuro" rotulo={frasePrazo}>
          <Texto suave>Pedidos como o seu costumam levar</Texto>
          <Texto tamanho={38} negrito cor={cores.destaque} style={{ letterSpacing: -0.8 }}>{previsao.data.faixa}</Texto>
          <Texto suave tamanho={15}>
            {previsao.data.fonte === 'modelo' ? 'Calculado com pedidos reais do 156, sem dados pessoais.' : 'Conta aproximada (o cálculo completo está fora do ar agora).'}
          </Texto>
          {lerEmVoz && <Botao variante="destaque" icone="ouvir" titulo="Ouvir" onPress={() => falar(frasePrazo)} />}
        </Cartao>
      )}
      {previsao.isError && (
        <Cartao tom="alerta">
          <Texto>Não consegui calcular agora. Tente de novo em instantes.</Texto>
        </Cartao>
      )}

      {tipo && <Enviar tipo={tipo} bairro={distrito ? nomeDoDistrito(distrito) : null} />}
    </Tela>
  );
}

const PRECISA = ['O endereço do problema, com número e CEP.', 'No final, responder um teste rápido de letras ou imagens. Essa parte é com você.'];

/** Passo 4: mandar o pedido de verdade, no formulário oficial do 156 (sem senha), e guardar o protocolo. */
function Enviar({ tipo, bairro }: { tipo: string; bairro: string | null }) {
  const { cores } = usePreferencias();
  const cliente = useQueryClient();
  const envio = ENVIO_156[tipo];
  const [abriu, setAbriu] = useState(false);
  const [copiou, setCopiou] = useState(false);
  const [protocolo, setProtocolo] = useState('');
  const [guardado, setGuardado] = useState<string>();
  if (!envio) return null;

  if (envio.tipo === 'telefone') {
    return (
      <>
        <Passo numero={4} titulo="Mandar para a Prefeitura" />
        <Cartao>
          <Texto>{envio.motivo} A ligação é gratuita e o atendente anota tudo para você.</Texto>
          <Botao icone="telefone" titulo="Ligar para o 156" onPress={() => Linking.openURL('tel:156')} />
        </Cartao>
      </>
    );
  }

  const descricao = descricaoPronta(tipo, bairro);
  return (
    <>
      <Passo numero={4} titulo="Mandar para a Prefeitura" />
      <Cartao rotulo="Como mandar o pedido">
        <Selo tom="sucesso" icone="cadeado" texto="Sem senha: não precisa entrar no gov.br" />
        <Texto>Vou abrir o formulário oficial do 156 ({envio.servico}). Tenha em mãos:</Texto>
        {PRECISA.map((p) => (
          <View key={p} style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}>
            <Icone nome="certo" tamanho={22} cor={cores.sucesso} />
            <Texto style={{ flex: 1 }}>{p}</Texto>
          </View>
        ))}
        <View style={{ backgroundColor: cores.campo, borderRadius: 18, padding: 14, gap: 2 }}>
          <Texto tamanho={15} suave>Descrição pronta para colar no formulário</Texto>
          <Texto tamanho={19} negrito selectable>{descricao}</Texto>
        </View>
        <Botao
          variante="secundario"
          icone={copiou ? 'certo' : 'copiar'}
          titulo={copiou ? 'Descrição copiada!' : 'Copiar descrição'}
          onPress={async () => {
            await Clipboard.setStringAsync(descricao);
            setCopiou(true);
          }}
        />
        <Botao
          icone="abrir"
          titulo="Abrir o site da Prefeitura"
          onPress={async () => {
            setAbriu(true);
            await WebBrowser.openBrowserAsync(envio.url);
          }}
        />
      </Cartao>

      {abriu && !guardado && (
        <Cartao tom="destaque">
          <Texto tamanho={20} negrito>Mandou? Guarde o número do protocolo</Texto>
          <Texto>No fim, o site mostra um número de protocolo. Escreva aqui para acompanhar depois.</Texto>
          <Campo rotulo="Número do protocolo" valor={protocolo} mudar={setProtocolo} dica="Ex.: 2026-0012345" />
          <Botao
            icone="documentos"
            titulo="Guardar em Documentos"
            desativado={!protocolo.trim()}
            onPress={() => {
              guardarProtocolo(protocolo.trim(), tipo);
              cliente.invalidateQueries({ queryKey: ['documentos'] });
              setGuardado(protocolo.trim());
            }}
          />
        </Cartao>
      )}

      {guardado && (
        <Cartao tom="escuro" rotulo={`Protocolo ${guardado} guardado em Documentos`}>
          <Selo tom="destaque" icone="certo" texto="Pedido guardado" />
          <Texto tamanho={24} negrito>Protocolo {guardado}</Texto>
          <Texto suave>Ele fica em Documentos. A Prefeitura avisa quando o pedido for resolvido.</Texto>
          <Botao variante="destaque" icone="documentos" titulo="Ver em Documentos" onPress={() => router.navigate('/documentos')} />
        </Cartao>
      )}
    </>
  );
}
