// SampaIA — lembrete físico para idosos (ESP32 no Wokwi).
// Mostra o próximo vencimento no display, acende o LED e toca o buzzer na véspera.
// Botão "Já paguei" marca o lembrete como concluído na API de Lembretes.
// Conversa por MQTT com o Node-RED da VM (infra/03_nodered.sh), que chama a API em localhost:
// nenhum lado precisa de URL pública nem de túnel.

#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// ---- Configuração ----
// DISPOSITIVO tem de ser igual ao SAMPAIA_DISPOSITIVO da VM (ver /etc/sampaia/sampaia.env).
// Funciona como senha dos tópicos: não use um código fácil de adivinhar.
const char* DISPOSITIVO = "casa-7f3k9q";
const char* MQTT_BROKER = "broker.hivemq.com";  // mesmo broker do Node-RED
const int MQTT_PORTA = 1883;
const char* WIFI_SSID = "Wokwi-GUEST";
const char* WIFI_SENHA = "";
const unsigned long INTERVALO_MS = 30UL * 60UL * 1000UL;  // pede o próximo a cada 30 min
const unsigned long ESPERA_RESPOSTA_MS = 15000;          // sem resposta: mostra o exemplo

// Se a API não responder, mostra este exemplo (dados simulados) para a demonstração não parar.
const char* EXEMPLO_JSON = "{\"id\":0,\"titulo\":\"Conta de água\",\"dias\":1,\"alerta\":true}";

// Definida antes de qualquer função: o Arduino insere os protótipos
// automáticos antes da primeira função, e eles precisam conhecer o tipo.
struct Lembrete {
  int id = -1;            // -1 = nada vencendo; 0 = exemplo embutido
  String titulo;
  int dias = 0;
  bool alerta = false;
};

// ---- Pinos (iguais ao diagram.json) ----
const int PINO_LED = 2;
const int PINO_BUZZER = 15;
const int PINO_BOTAO = 4;

Adafruit_SSD1306 tela(128, 64, &Wire, -1);
WiFiClient rede;
PubSubClient mqtt(rede);

// Tópicos: sampaia/<DISPOSITIVO>/pedir | concluir | lembrete | status
String topico(const char* acao) { return String("sampaia/") + DISPOSITIVO + "/" + acao; }

Lembrete atual;
unsigned long ultimaConsulta = 0;
unsigned long pediuEm = 0;
bool recebeu = false;

// O display usa fonte sem acentos: "água" vira "AGUA".
String semAcento(String s) {
  const char* de[] = {"á","à","â","ã","é","ê","í","ó","ô","õ","ú","ç","Á","À","Â","Ã","É","Ê","Í","Ó","Ô","Õ","Ú","Ç"};
  const char* para[] = {"a","a","a","a","e","e","i","o","o","o","u","c","A","A","A","A","E","E","I","O","O","O","U","C"};
  for (int i = 0; i < 24; i++) s.replace(de[i], para[i]);
  return s;
}

String frase(const Lembrete& l) {
  if (l.dias <= 0) return "vence HOJE";
  if (l.dias == 1) return "vence AMANHA";
  return "vence em " + String(l.dias) + " dias";
}

void mostrar(const String& linha1, const String& linha2, const String& rodape) {
  tela.clearDisplay();
  tela.setTextColor(SSD1306_WHITE);
  tela.setTextSize(2);
  tela.setCursor(0, 0);
  tela.println(linha1);
  tela.setTextSize(1);
  tela.setCursor(0, 36);
  tela.println(linha2);
  tela.setCursor(0, 54);
  tela.println(rodape);
  tela.display();
}

bool lerJson(const String& json, Lembrete& l) {
  JsonDocument doc;
  if (deserializeJson(doc, json)) return false;
  l.id = doc["id"] | 0;
  l.titulo = semAcento(doc["titulo"] | "Conta");
  l.titulo.toUpperCase();
  l.dias = doc["dias"] | 0;
  l.alerta = doc["alerta"] | false;
  return true;
}

// Pede ao Node-RED o próximo lembrete; a resposta chega em recebida().
void consultar() {
  if (mqtt.connected()) {
    mqtt.publish(topico("pedir").c_str(), "1");
    Serial.println("MQTT -> pedir");
  }
  pediuEm = millis();
}

// Mensagem do Node-RED em sampaia/<DISPOSITIVO>/lembrete (fica retida no broker).
void recebida(char* topicoMsg, byte* dados, unsigned int tamanho) {
  String json;
  for (unsigned int i = 0; i < tamanho; i++) json += (char)dados[i];
  Serial.printf("MQTT <- %s: %s\n", topicoMsg, json.c_str());
  Lembrete novo;
  if (!lerJson(json, novo)) return;
  atual = novo;
  recebeu = true;
  atualizarTela();
}

void conectarMqtt() {
  if (WiFi.status() != WL_CONNECTED || mqtt.connected()) return;
  String id = String("sampaia-esp32-") + String((uint32_t)ESP.getEfuseMac(), HEX);
  // "offline" retido como last will: o Node-RED mostra se o aparelho caiu.
  if (mqtt.connect(id.c_str(), topico("status").c_str(), 1, true, "offline")) {
    mqtt.publish(topico("status").c_str(), "online", true);
    mqtt.subscribe(topico("lembrete").c_str(), 1);
    Serial.println("MQTT conectado");
    consultar();
  } else {
    Serial.printf("MQTT falhou (estado %d)\n", mqtt.state());
  }
}

void atualizarTela() {
  if (atual.id < 0) {
    digitalWrite(PINO_LED, LOW);
    mostrar("TUDO EM", "DIA!", "Nada vencendo agora");
    return;
  }
  digitalWrite(PINO_LED, atual.alerta ? HIGH : LOW);
  mostrar(atual.titulo.substring(0, 10), frase(atual), atual.id == 0 ? "(exemplo, sem conexao)" : "Pagou? Aperte o botao");
  if (atual.alerta) tocar();
}

void tocar() {
  for (int i = 0; i < 3; i++) {
    tone(PINO_BUZZER, 880, 200);
    delay(350);
  }
}

void jaPaguei() {
  if (atual.id < 0) return;
  bool enviou = false;
  if (atual.id > 0 && mqtt.connected()) {
    String corpo = "{\"id\":" + String(atual.id) + "}";
    enviou = mqtt.publish(topico("concluir").c_str(), corpo.c_str());
    Serial.println("MQTT -> concluir " + corpo);
  }
  digitalWrite(PINO_LED, LOW);
  mostrar("OBRIGADO!", "Marcado como pago.", "");
  tone(PINO_BUZZER, 1320, 150);
  delay(2000);
  // O Node-RED marca como pago na API e publica o próximo lembrete sozinho.
  if (enviou) mostrar("OBRIGADO!", "Buscando o proximo...", "");
  else atualizarTela();
}

void setup() {
  Serial.begin(115200);
  pinMode(PINO_LED, OUTPUT);
  pinMode(PINO_BUZZER, OUTPUT);
  pinMode(PINO_BOTAO, INPUT_PULLUP);

  tela.begin(SSD1306_SWITCHCAPVCC, 0x3C);
  mostrar("SampaIA", "Conectando...", "");

  WiFi.begin(WIFI_SSID, WIFI_SENHA);
  for (int i = 0; i < 40 && WiFi.status() != WL_CONNECTED; i++) delay(250);
  Serial.println(WiFi.status() == WL_CONNECTED ? "Wi-Fi conectado" : "Sem Wi-Fi: usando exemplo");

  mqtt.setServer(MQTT_BROKER, MQTT_PORTA);
  mqtt.setCallback(recebida);
  mqtt.setBufferSize(512);
  conectarMqtt();
  pediuEm = millis();
  ultimaConsulta = millis();
}

void loop() {
  if (!mqtt.connected()) {
    static unsigned long ultimaTentativa = 0;
    if (millis() - ultimaTentativa > 5000) { ultimaTentativa = millis(); conectarMqtt(); }
  }
  mqtt.loop();

  // Sem resposta do Node-RED (broker ou VM fora do ar): exemplo embutido, para a demo não parar.
  if (!recebeu && millis() - pediuEm > ESPERA_RESPOSTA_MS && atual.id != 0) {
    lerJson(EXEMPLO_JSON, atual);
    atualizarTela();
  }
  if (digitalRead(PINO_BOTAO) == LOW) {
    delay(50);  // debounce
    if (digitalRead(PINO_BOTAO) == LOW) {
      jaPaguei();
      while (digitalRead(PINO_BOTAO) == LOW) delay(10);
    }
  }
  if (millis() - ultimaConsulta >= INTERVALO_MS) {
    consultar();
    ultimaConsulta = millis();
  }
  delay(20);
}
