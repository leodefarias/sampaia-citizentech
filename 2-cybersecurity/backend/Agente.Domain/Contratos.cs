namespace Agente.Domain;

public enum TipoAcao { Navegar, Clicar, Preencher, AguardarUsuario, Confirmar, Concluir }

/// <summary>Ação que a extensão executa. Valor é sempre um marcador ([SQL]) ou texto sem dado pessoal.</summary>
public record AcaoAgente(TipoAcao Acao, string? Seletor, string? Valor, bool ExigeConfirmacao, string Explicacao)
{
    public static AcaoAgente EsperarUsuario(string explicacao) => new(TipoAcao.AguardarUsuario, null, null, false, explicacao);
}

/// <summary>Retrato da página enviado pela extensão (já mascarado por ela).</summary>
public record RetratoPagina(string Url, string? Titulo, string Conteudo);

public record ResultadoMascara(string Texto, bool Bloqueado, IReadOnlyList<string> Encontrados)
{
    public bool TinhaPii => Bloqueado || Encontrados.Count > 0;
}

/// <summary>Sanitizador da taint analysis: todo texto vindo do DOM ou do chat passa aqui antes de LLM, log ou banco.</summary>
public interface IPiiMasker
{
    ResultadoMascara Mascarar(string texto);
}

public record PedidoProximaAcao(
    string Servico, string UrlBase, string Intencao, IReadOnlyList<string> Plano,
    IReadOnlyList<string> PassosFeitos, string Instrucoes, string Url, string ConteudoMascarado);

public interface ILlmClient
{
    Task<AcaoAgente> ProximaAcaoAsync(PedidoProximaAcao pedido, CancellationToken ct);

    /// <summary>Traduz um evento técnico de segurança para uma frase simples ao cidadão.</summary>
    Task<string?> TraduzirEventoAsync(EventoAuditoria evento, CancellationToken ct);
}

public record LembreteNovo(string Titulo, DateOnly DataVencimento, int? DocumentoId);

/// <summary>Cliente HTTP da API de Lembretes (outro serviço, SOA).</summary>
public interface ILembretesClient
{
    Task CriarAsync(LembreteNovo lembrete, CancellationToken ct);
}

public record PedidoPrevisao(string TipoServico, string? Distrito, DateOnly? DataAbertura);
public record PrevisaoPrazo(string Faixa, string Classe, string Fonte);

/// <summary>Cliente do serviço Python de ML (/prever-prazo).</summary>
public interface IPrevisaoPrazoClient
{
    Task<PrevisaoPrazo> PreverAsync(PedidoPrevisao pedido, CancellationToken ct);
}
