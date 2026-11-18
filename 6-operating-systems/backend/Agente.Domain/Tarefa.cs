namespace Agente.Domain;

public enum EstadoTarefa { Planejando, Navegando, AguardandoLogin, AguardandoConfirmacao, Concluida, Falhou }

public class TransicaoInvalidaException(EstadoTarefa de, EstadoTarefa para)
    : InvalidOperationException($"A tarefa não pode ir de {de} para {para}.");

public class Tarefa
{
    // Falhou é permitido a partir de qualquer estado não final (tratado em MudarPara).
    private static readonly Dictionary<EstadoTarefa, EstadoTarefa[]> Transicoes = new()
    {
        [EstadoTarefa.Planejando] = [EstadoTarefa.Navegando],
        [EstadoTarefa.Navegando] = [EstadoTarefa.AguardandoLogin, EstadoTarefa.AguardandoConfirmacao, EstadoTarefa.Concluida],
        [EstadoTarefa.AguardandoLogin] = [EstadoTarefa.Navegando],
        [EstadoTarefa.AguardandoConfirmacao] = [EstadoTarefa.Navegando, EstadoTarefa.Concluida],
        [EstadoTarefa.Concluida] = [],
        [EstadoTarefa.Falhou] = [],
    };

    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }
    /// <summary>Pedido do usuário, já mascarado.</summary>
    public required string Intencao { get; set; }
    public EstadoTarefa Estado { get; private set; } = EstadoTarefa.Planejando;
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public List<PassoTarefa> Passos { get; set; } = [];
    public List<Documento> Documentos { get; set; } = [];

    public bool Finalizada => Estado is EstadoTarefa.Concluida or EstadoTarefa.Falhou;

    public static bool PodeIr(EstadoTarefa de, EstadoTarefa para) =>
        para == EstadoTarefa.Falhou ? de is not (EstadoTarefa.Concluida or EstadoTarefa.Falhou) : Transicoes[de].Contains(para);

    public void MudarPara(EstadoTarefa novo)
    {
        if (!PodeIr(Estado, novo)) throw new TransicaoInvalidaException(Estado, novo);
        Estado = novo;
    }
}
