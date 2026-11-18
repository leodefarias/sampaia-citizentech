# Lembrete físico para idosos (ESP32 no Wokwi)

Um ESP32 que fica na geladeira ou na sala e avisa os vencimentos sem precisar de celular.

| Peça | Pino |
|---|---|
| Display OLED SSD1306 (I2C) | SDA 21, SCL 22 |
| LED vermelho + resistor 220 Ω | 2 |
| Buzzer | 15 |
| Botão "Já paguei" | 4 (INPUT_PULLUP) |

## Funcionamento

1. Conecta pelo Wi-Fi `Wokwi-GUEST` ao broker MQTT e recebe o próximo lembrete em `sampaia/<DISPOSITIVO>/lembrete`. Quem consulta a API é o Node-RED da VM (`infra/03_nodered.sh`), ao iniciar, a cada 30 min e quando o ESP32 publica em `.../pedir`.
2. Mostra, por exemplo, "CONTA DE AGUA / vence em 2 dias" (o display não tem acentos).
3. Na véspera (`alerta: true` na resposta) acende o LED e toca o buzzer 3 vezes.
4. O botão **Já paguei** publica `{"id": N}` em `.../concluir`. O Node-RED marca como pago na API e devolve o próximo.
5. Sem resposta em 15 s (broker ou VM fora do ar), usa um **JSON de exemplo embutido** e mostra "(exemplo, sem conexao)".

## Como montar no Wokwi

1. Em [wokwi.com](https://wokwi.com), crie um projeto **ESP32**.
2. Substitua `sketch.ino` e `diagram.json` pelos desta pasta.
3. Em **Library Manager**, adicione as bibliotecas de `libraries.txt`.
4. Na VM, rode `sudo ./infra/03_nodered.sh`. O código do dispositivo já é o mesmo dos dois lados (`casa-7f3k9q`, no topo do `sketch.ino` e no `/etc/sampaia/sampaia.env`). `MQTT_BROKER` deve ser o mesmo dos dois lados (padrão `broker.hivemq.com`). Não precisa de URL pública nem de túnel.
5. Inicie a simulação. O monitor serial mostra cada mensagem MQTT ("MQTT -> pedir", "MQTT <- ...").

**Projeto pronto no Wokwi:** https://wokwi.com/projects/480882620502191105 (basta abrir e iniciar a simulação).

**Entregáveis:** link do projeto no Wokwi (https://wokwi.com/projects/480882620502191105), vídeo de até 4 min (https://youtu.be/aM7IXJtgf8U · [`videos/iot-esp32-wokwi.mp4`](../videos/iot-esp32-wokwi.mp4), gravado com este circuito rodando no Wokwi) e `integrantes.txt` preenchido.
