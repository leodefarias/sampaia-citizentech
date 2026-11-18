using Agente.Domain;
using Agente.Infra;
using Microsoft.EntityFrameworkCore;

namespace Agente.Api;

public record NovaTarefa(string Intencao, int? ServicoId, int? UsuarioId);
public record TarefaCriada(Tarefa Tarefa, IReadOnlyList<string> Plano);
public record Confirmacao(bool Confirmado);
public record Falha(string Motivo);
public record BloqueioExtensao(int? TarefaId, string Sink, List<string> Encontrados);

public class TarefaNaoEncontradaException(int id) : Exception($"Tarefa {id} não encontrada.");
public class PedidoInvalidoException(string mensagem) : Exception(mensagem);

/// <summary>Orquestra tarefa, conector e auditoria. Todo texto vindo de fora passa pelo IPiiMasker antes de ir ao banco.</summary>
public class TarefaService(AgenteDbContext db, IConectorFactory fabrica, IPiiMasker masker, ILembretesClient lembretes, ILlmClient llm)
{
    public async Task<TarefaCriada> CriarAsync(NovaTarefa pedido, CancellationToken ct)
    {
        var mascara = masker.Mascarar(pedido.Intencao);
        if (mascara.TinhaPii)
            await AuditarAsync(null, mascara.Bloqueado ? "SenhaNoChat" : "PiiNoChat", mascara.Bloqueado ? NivelRisco.Alto : NivelRisco.Baixo, mascara.Encontrados, ct);
        if (mascara.Bloqueado)
        {
            await db.SaveChangesAsync(ct);
            throw new PedidoInvalidoException("Não escreva senhas aqui. Eu nunca preciso da sua senha.");
        }

        IConector? conector;
        if (pedido.ServicoId is { } sid)
        {
            var chave = await db.Servicos.Where(s => s.Id == sid).Select(s => s.Chave).FirstOrDefaultAsync(ct)
                ?? throw new PedidoInvalidoException("Serviço não encontrado.");
            conector = fabrica.PorChave(chave);
        }
        else conector = fabrica.PorIntencao(mascara.Texto);

        if (conector is null)
            throw new PedidoInvalidoException("Não entendi qual serviço você quer. Tente: conta de água, IPTU, IPVA, pedido no 156 ou agendar no Poupatempo.");

        var servico = await db.Servicos.SingleAsync(s => s.Chave == conector.Chave, ct);
        var tarefa = new Tarefa { UsuarioId = pedido.UsuarioId ?? 1, ServicoId = servico.Id, Servico = servico, Intencao = mascara.Texto };
        db.Tarefas.Add(tarefa);
        await db.SaveChangesAsync(ct);

        var plano = await conector.MontarPlanoAsync(tarefa, ct);
        tarefa.MudarPara(EstadoTarefa.Navegando);
        await db.SaveChangesAsync(ct);
        return new TarefaCriada(tarefa, plano);
    }

    public async Task<AcaoAgente> ProximoPassoAsync(int id, RetratoPagina retrato, CancellationToken ct)
    {
        var tarefa = await CarregarAsync(id, ct);
        var conector = fabrica.PorChave(tarefa.Servico!.Chave)!;

        // A extensão já deveria ter mascarado. Se ainda havia PII, registra (taint: fonte DOM chegou suja ao servidor).
        var mascara = masker.Mascarar(retrato.Conteudo);
        if (mascara.TinhaPii)
            await AuditarAsync(id, mascara.Bloqueado ? "SenhaNaPagina" : "PiiSemMascaraDaExtensao", mascara.Bloqueado ? NivelRisco.Alto : NivelRisco.Medio, mascara.Encontrados, ct);

        if (ConectorBase.EhLogin(retrato.Url) && tarefa.Estado == EstadoTarefa.Navegando)
            tarefa.MudarPara(EstadoTarefa.AguardandoLogin);
        else if (tarefa.Estado != EstadoTarefa.Navegando)
            throw new TransicaoInvalidaException(tarefa.Estado, EstadoTarefa.Navegando);

        var acao = await conector.ProximoPassoAsync(tarefa, retrato, ct);

        Documento? novo = null;
        if (acao.Acao == TipoAcao.Confirmar || acao.ExigeConfirmacao)
            tarefa.MudarPara(EstadoTarefa.AguardandoConfirmacao);
        else if (acao.Acao == TipoAcao.Concluir)
        {
            tarefa.MudarPara(EstadoTarefa.Concluida);
            novo = await conector.ExtrairDocumentoAsync(tarefa, retrato, ct);
            if (novo is not null) tarefa.Documentos.Add(novo);
        }

        RegistrarPasso(tarefa, acao.Acao, acao.Seletor, acao.ExigeConfirmacao, acao.Explicacao);
        await db.SaveChangesAsync(ct);

        // Documento com vencimento vira lembrete no outro serviço (app e ESP32 leem de lá).
        if (novo?.Vencimento is { } vencimento)
            await lembretes.CriarAsync(new LembreteNovo(tarefa.Servico.Nome, vencimento, novo.Id), ct);
        return acao;
    }

    public Task<Tarefa> PausarAsync(int id, CancellationToken ct) => MudarAsync(id, EstadoTarefa.AguardandoLogin, ct);
    public Task<Tarefa> RetomarAsync(int id, CancellationToken ct) => MudarAsync(id, EstadoTarefa.Navegando, ct);

    public async Task<Tarefa> ConfirmarAsync(int id, bool confirmado, CancellationToken ct)
    {
        // Recusa = nada é gerado, tarefa encerra.
        var tarefa = await MudarAsync(id, confirmado ? EstadoTarefa.Navegando : EstadoTarefa.Falhou, ct, salvar: false);
        RegistrarPasso(tarefa, TipoAcao.Confirmar, null, true, confirmado ? "Você confirmou." : "Você não confirmou. Nada foi gerado.");
        await db.SaveChangesAsync(ct);
        return tarefa;
    }

    public async Task<Tarefa> FalharAsync(int id, string motivo, CancellationToken ct)
    {
        var tarefa = await MudarAsync(id, EstadoTarefa.Falhou, ct, salvar: false);
        RegistrarPasso(tarefa, TipoAcao.AguardarUsuario, null, false, motivo);
        await db.SaveChangesAsync(ct);
        return tarefa;
    }

    public async Task<Tarefa> CarregarAsync(int id, CancellationToken ct) =>
        await db.Tarefas.Include(t => t.Servico).Include(t => t.Passos).Include(t => t.Documentos)
            .FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new TarefaNaoEncontradaException(id);

    private async Task<Tarefa> MudarAsync(int id, EstadoTarefa estado, CancellationToken ct, bool salvar = true)
    {
        var tarefa = await CarregarAsync(id, ct);
        tarefa.MudarPara(estado);
        if (salvar) await db.SaveChangesAsync(ct);
        return tarefa;
    }

    private void RegistrarPasso(Tarefa tarefa, TipoAcao acao, string? seletor, bool exigeConfirmacao, string resultado) =>
        tarefa.Passos.Add(new PassoTarefa { Acao = acao, Seletor = seletor, ExigeConfirmacao = exigeConfirmacao, Resultado = masker.Mascarar(resultado).Texto });

    /// <summary>A extensão avisa que mascarou PII no navegador (fonte DOM ou chat) antes de enviar.</summary>
    public async Task<EventoAuditoria> RegistrarBloqueioDaExtensaoAsync(BloqueioExtensao b, CancellationToken ct)
    {
        // Só nomes de regras conhecidas entram no detalhe: nada que a extensão mande vira texto livre no banco.
        var conhecidas = b.Encontrados.Where(RegrasConhecidas.Contains).ToList();
        var sink = b.Sink is "llm" or "storage" or "api" ? b.Sink : "desconhecido";
        // Senha no pedido do chat: a extensão bloqueia e nem envia o pedido, então o alerta alto nasce aqui (CT-25).
        var senhaNoChat = sink == "api" && conhecidas.Contains("senha");
        var e = await AuditarAsync(b.TarefaId, senhaNoChat ? "SenhaNoChat" : "PiiBloqueadaNaExtensao",
            senhaNoChat ? NivelRisco.Alto : NivelRisco.Baixo, conhecidas, ct, $"Sink: {sink}. ");
        await db.SaveChangesAsync(ct);
        return e;
    }

    private static readonly HashSet<string> RegrasConhecidas =
        ["senha", "pix-copia-e-cola", "boleto-bancario", "boleto-arrecadacao", "cns", "cpf", "cartao", "email", "telefone", "rg", "cep", "logradouro", "saude", "documento-numerico"];

    // Detalhe guarda só os tipos encontrados, nunca o valor. A tradução em frase simples vem do LLM.
    private async Task<EventoAuditoria> AuditarAsync(int? tarefaId, string tipo, NivelRisco nivel, IEnumerable<string> encontrados, CancellationToken ct, string prefixo = "")
    {
        var e = new EventoAuditoria { TarefaId = tarefaId, Tipo = tipo, NivelRisco = nivel, Detalhe = $"{prefixo}Encontrado: {string.Join(", ", encontrados.Distinct())}" };
        e.Traducao = await llm.TraduzirEventoAsync(e, ct) ?? Traducoes.Frase(e);
        db.Eventos.Add(e);
        return e;
    }
}
