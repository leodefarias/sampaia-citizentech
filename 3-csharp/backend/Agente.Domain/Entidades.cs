namespace Agente.Domain;

public enum ModoServico { Publico, Login, Anonimo }
public enum TipoDocumento { Boleto, Pix, Protocolo, Agendamento }
public enum NivelRisco { Baixo, Medio, Alto }

public class Usuario
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public string? PreferenciasAcessibilidade { get; set; }
}

public class Servico
{
    public int Id { get; set; }
    /// <summary>Chave usada pela fábrica para achar o conector (ex.: "iptu").</summary>
    public required string Chave { get; set; }
    public required string Nome { get; set; }
    public required string Orgao { get; set; }
    public ModoServico Modo { get; set; }
    public required string UrlBase { get; set; }
    /// <summary>Hosts que o agente nunca lê (login, pagamento), separados por vírgula.</summary>
    public string? DominiosSensiveis { get; set; }
}

public class PassoTarefa
{
    public int Id { get; set; }
    public int TarefaId { get; set; }
    public TipoAcao Acao { get; set; }
    public string? Seletor { get; set; }
    public bool ExigeConfirmacao { get; set; }
    /// <summary>Explicação dada ao usuário ou resultado relatado; sempre texto mascarado.</summary>
    public string? Resultado { get; set; }
    public DateTime Em { get; set; } = DateTime.UtcNow;
}

public class Documento
{
    public int Id { get; set; }
    public int TarefaId { get; set; }
    public TipoDocumento Tipo { get; set; }
    public decimal? Valor { get; set; }
    public DateOnly? Vencimento { get; set; }
    /// <summary>Protocolo ou nº do agendamento. Linha digitável e Pix não entram aqui: são mascarados e ficam só no computador.</summary>
    public string? Codigo { get; set; }
}

public class EventoAuditoria
{
    public int Id { get; set; }
    public int? TarefaId { get; set; }
    public required string Tipo { get; set; }
    public NivelRisco NivelRisco { get; set; }
    /// <summary>Sempre mascarado antes de gravar (sink da taint analysis).</summary>
    public required string Detalhe { get; set; }
    /// <summary>Frase simples gerada pelo LLM a partir do evento (que só contém tipos, nunca valores).</summary>
    public string? Traducao { get; set; }
    public DateTime Em { get; set; } = DateTime.UtcNow;

    public int Peso => NivelRisco switch { NivelRisco.Alto => 5, NivelRisco.Medio => 3, _ => 1 };
}
