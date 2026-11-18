using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Agente.Domain;
using Anthropic;
using Anthropic.Models.Beta.Messages;

namespace Agente.Infra;

public static class Json
{
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

/// <summary>LLM determinístico para testes e demo offline: abre o site e depois conclui.</summary>
public class LlmFake : ILlmClient
{
    public Task<AcaoAgente> ProximaAcaoAsync(PedidoProximaAcao p, CancellationToken ct) =>
        Task.FromResult(p.PassosFeitos.Count == 0
            ? new AcaoAgente(TipoAcao.Navegar, null, p.UrlBase, false, $"Vou abrir o site: {p.Servico}.")
            : new AcaoAgente(TipoAcao.Concluir, null, null, false, "Pronto, terminei esta parte."));

    public Task<string?> TraduzirEventoAsync(EventoAuditoria e, CancellationToken ct) => Task.FromResult<string?>(Traducoes.Frase(e));
}

/// <summary>Frases prontas: usadas pelo LLM fake e como plano B se o Claude falhar.</summary>
public static class Traducoes
{
    private static readonly Dictionary<string, string> Nomes = new()
    {
        ["cpf"] = "seu CPF", ["email"] = "seu e-mail", ["telefone"] = "seu telefone", ["cns"] = "seu Cartão SUS",
        ["saude"] = "uma informação de saúde", ["cartao"] = "o número de um cartão", ["boleto-bancario"] = "o código de um boleto",
        ["boleto-arrecadacao"] = "o código de uma conta", ["pix-copia-e-cola"] = "um código Pix", ["rg"] = "seu RG",
        ["documento-numerico"] = "o número de um documento", ["cep"] = "seu endereço", ["logradouro"] = "seu endereço", ["senha"] = "uma senha",
    };

    public static string Frase(EventoAuditoria e)
    {
        var i = e.Detalhe.IndexOf("Encontrado:", StringComparison.Ordinal);
        var achados = i < 0 ? [] : e.Detalhe[(i + "Encontrado:".Length)..].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var itens = achados.Select(a => Nomes.GetValueOrDefault(a, "um dado pessoal")).Distinct().ToList();
        var oque = itens.Count switch
        {
            0 => "um dado pessoal",
            1 => itens[0],
            _ => string.Join(", ", itens[..^1]) + " e " + itens[^1],
        };
        return e.Tipo switch
        {
            "PiiBloqueadaNaExtensao" => $"Escondemos {oque} antes de qualquer coisa sair do seu computador.",
            "PiiSemMascaraDaExtensao" => $"Bloqueamos o envio de {oque} para fora do seu computador.",
            "PiiNoChat" => $"Você escreveu {oque} na conversa. Escondemos antes de guardar.",
            "SenhaNoChat" or "SenhaNaPagina" => "Apareceu uma senha. Nós não lemos nem guardamos senhas.",
            _ => $"Protegemos {oque}.",
        };
    }
}

/// <summary>Claude escolhe a próxima ação. Recebe só texto já mascarado; a chave vem de ANTHROPIC_API_KEY.</summary>
public class ClaudeLlmClient(AnthropicClient cliente, ILogger<ClaudeLlmClient> log) : ILlmClient
{
    // Haiku 5.5: barato para um passo por página. Não tem fallback no servidor (não mandar `fallbacks`).
    public const string Modelo = "claude-haiku-5-5";

    private const string Sistema = """
        Você é a Paulinha, agente que ajuda idosos a usar serviços públicos de São Paulo num navegador.
        A cada passo você recebe o pedido, o plano, os passos já feitos e o texto da página atual, e devolve UMA ação.
        Regras:
        - Dados pessoais aparecem como marcadores ([CPF], [SQL], [DOC]...). Para preencher, use o marcador como valor; nunca invente números.
        - Captcha, login ou qualquer coisa que só a pessoa pode fazer: acao = aguardarUsuario.
        - Antes de gerar boleto, pagar, enviar pedido ou confirmar agendamento: acao = confirmar e exigeConfirmacao = true.
        - seletor é um seletor CSS presente na página; use null quando não se aplica.
        - Aviso de cookies: recuse (botão "Rejeitar"). Pop-up de comunicado: feche.
        - Nunca preencha CPF, senha ou código de login: o login é sempre da pessoa (aguardarUsuario).
        - Quando o objetivo foi atingido: acao = concluir.
        - explicacao: uma frase curta, simples e gentil, em português, dizendo o que você vai fazer.
          Fale com a pessoa por "você"; não presuma gênero (nada de "a senhora" ou "o senhor").
        """;

    private static readonly Dictionary<string, JsonElement> Esquema = new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            acao = new { type = "string", @enum = new[] { "navegar", "clicar", "preencher", "aguardarUsuario", "confirmar", "concluir" } },
            seletor = new { type = new[] { "string", "null" } },
            valor = new { type = new[] { "string", "null" } },
            exigeConfirmacao = new { type = "boolean" },
            explicacao = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "acao", "seletor", "valor", "exigeConfirmacao", "explicacao" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    public async Task<AcaoAgente> ProximaAcaoAsync(PedidoProximaAcao p, CancellationToken ct)
    {
        var usuario = $"""
            Serviço: {p.Servico} ({p.UrlBase})
            Pedido: {p.Intencao}
            Plano: {string.Join(" | ", p.Plano)}
            Passos feitos: {(p.PassosFeitos.Count == 0 ? "nenhum" : string.Join(" | ", p.PassosFeitos))}
            Dicas do site: {p.Instrucoes}
            URL atual: {p.Url}
            Página:
            {p.ConteudoMascarado}
            """;

        var resposta = await cliente.Beta.Messages.Create(new MessageCreateParams
        {
            Model = Modelo,
            MaxTokens = 8000, // o raciocínio conta no max_tokens; folga para não cortar o JSON
            System = Sistema,
            OutputConfig = new BetaOutputConfig { Effort = "medium", Format = new BetaJsonOutputFormat { Schema = Esquema } },
            Messages = [new BetaUserMessageParam { Content = usuario }],
        }, ct);
        // Só modelo e contagens: nada da página vai para o log.
        log.LogInformation("LLM {Modelo} passo: entrada {Entrada} saida {Saida} tokens, stop {Stop}",
            resposta.Model, resposta.Usage.InputTokens, resposta.Usage.OutputTokens, resposta.StopReason);

        if (resposta.StopReason == "refusal")
            return AcaoAgente.EsperarUsuario("Não consegui seguir sozinha. Vou mostrar o passo a passo para você fazer.");

        var texto = string.Concat(resposta.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
        return JsonSerializer.Deserialize<AcaoAgente>(texto, Json.Opcoes)
            ?? AcaoAgente.EsperarUsuario("Não entendi esta página. Pode me ajudar?");
    }

    /// <summary>O evento só tem tipo, nível e nomes de regras (nunca valores), então pode ir ao LLM.</summary>
    public async Task<string?> TraduzirEventoAsync(EventoAuditoria e, CancellationToken ct)
    {
        try { return await TraduzirComClaudeAsync(e, ct); }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            // Falha do LLM (rede, 429, 5xx) não pode derrubar o passo da tarefa: usa a frase pronta.
            return Traducoes.Frase(e);
        }
    }

    private async Task<string> TraduzirComClaudeAsync(EventoAuditoria e, CancellationToken ct)
    {
        var resposta = await cliente.Beta.Messages.Create(new MessageCreateParams
        {
            Model = Modelo,
            MaxTokens = 1500,
            System = """
                Você explica eventos de segurança para idosos, em uma frase curta, calma e sem termos técnicos, em português.
                Diga só que o dado ficou protegido. Nunca diga que alguém ou algum programa tentou pegar, roubar ou guardar o dado. Não invente detalhes.
                Onde o dado foi protegido (Sink): storage = histórico da conversa neste computador; llm = assistente de inteligência artificial; api = servidor da Paulinha.
                """,
            OutputConfig = new BetaOutputConfig { Effort = "low" },
            Messages = [new BetaUserMessageParam { Content = $"Evento: {e.Tipo}. Nível: {e.NivelRisco}. {e.Detalhe}" }],
        }, ct);
        var frase = string.Concat(resposta.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text)).Trim();
        return resposta.StopReason == "refusal" || frase == "" ? Traducoes.Frase(e) : frase;
    }
}
