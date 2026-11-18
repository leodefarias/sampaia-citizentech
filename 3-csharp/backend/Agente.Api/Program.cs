using System.Text.Json;
using System.Text.Json.Serialization;
using Agente.Api;
using Agente.Domain;
using Agente.Infra;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfra(
    oracle: builder.Configuration.GetConnectionString("Oracle"),
    sqlite: builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=agente.db",
    usarClaude: !string.IsNullOrWhiteSpace(builder.Configuration["ANTHROPIC_API_KEY"]),
    urlLembretes: builder.Configuration["Lembretes:Url"] ?? "http://localhost:5010",
    urlPrevisao: builder.Configuration["Previsao:Url"] ?? "http://localhost:8010");
builder.Services.AddScoped<TarefaService>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgenteDbContext>();
    // SQLite (dev/testes) cria o banco direto; Oracle usa as migrações (dotnet ef database update).
    if (db.Database.IsSqlite()) db.Database.EnsureCreated();
}

// Erros de domínio viram respostas simples para o usuário.
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception e) when (e is TarefaNaoEncontradaException or PedidoInvalidoException or TransicaoInvalidaException)
    {
        var status = e switch { TarefaNaoEncontradaException => 404, PedidoInvalidoException => 400, _ => 409 };
        await Results.Problem(e.Message, statusCode: status).ExecuteAsync(ctx);
    }
});
app.UseCors();
app.MapOpenApi();
app.MapScalarApiReference();

var api = app.MapGroup("/api");

api.MapGet("/servicos", (AgenteDbContext db, CancellationToken ct) => db.Servicos.AsNoTracking().ToListAsync(ct))
    .WithSummary("Lista o catálogo de serviços");

var tarefas = api.MapGroup("/tarefas");
tarefas.MapGet("/", (AgenteDbContext db, CancellationToken ct) =>
        db.Tarefas.AsNoTracking().Include(t => t.Servico).Include(t => t.Passos)
            .OrderByDescending(t => t.CriadaEm).Take(50).ToListAsync(ct))
    .WithSummary("Histórico de tarefas do agente (mais recentes primeiro)");
tarefas.MapPost("/", (NovaTarefa p, TarefaService s, CancellationToken ct) => s.CriarAsync(p, ct))
    .WithSummary("Cria a tarefa a partir do pedido em texto livre e devolve o plano");
tarefas.MapGet("/{id:int}", (int id, TarefaService s, CancellationToken ct) => s.CarregarAsync(id, ct))
    .WithSummary("Estado e passos da tarefa");
tarefas.MapPost("/{id:int}/proximo-passo", (int id, RetratoPagina r, TarefaService s, CancellationToken ct) => s.ProximoPassoAsync(id, r, ct))
    .WithSummary("Recebe o retrato mascarado da página e devolve a próxima ação");
tarefas.MapPost("/{id:int}/pausar", (int id, TarefaService s, CancellationToken ct) => s.PausarAsync(id, ct))
    .WithSummary("Pausa para o usuário entrar no gov.br");
tarefas.MapPost("/{id:int}/retomar", (int id, TarefaService s, CancellationToken ct) => s.RetomarAsync(id, ct))
    .WithSummary("Retoma depois do login");
tarefas.MapPost("/{id:int}/confirmar", (int id, Confirmacao c, TarefaService s, CancellationToken ct) => s.ConfirmarAsync(id, c.Confirmado, ct))
    .WithSummary("Usuário confirma (ou recusa) a ação final");
tarefas.MapPost("/{id:int}/falhar", (int id, Falha f, TarefaService s, CancellationToken ct) => s.FalharAsync(id, f.Motivo, ct))
    .WithSummary("Encerra a tarefa (tempo esgotado, elemento não encontrado, usuário cancelou)");
tarefas.MapGet("/falhas-por-servico", (AgenteDbContext db, CancellationToken ct) =>
        db.Tarefas.Where(t => t.Estado == EstadoTarefa.Falhou)
            .GroupBy(t => t.Servico!.Nome)
            .Select(g => new { Servico = g.Key, Falhas = g.Count() })
            .OrderByDescending(x => x.Falhas)
            .ToListAsync(ct))
    .WithSummary("Quantas tarefas falharam em cada serviço");

api.MapPost("/previsao-prazo", async (PedidoPrevisao p, IPrevisaoPrazoClient previsao, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(p.TipoServico)
            ? Results.Problem("Informe o tipo do pedido.", statusCode: 400)
            : Results.Ok(await previsao.PreverAsync(p, ct)))
    .WithSummary("Previsão de prazo do SP156 (modelo de ML; estimativa simples se o serviço estiver fora)");

api.MapGet("/documentos", (AgenteDbContext db, CancellationToken ct) =>
        db.Documentos.AsNoTracking().OrderBy(d => d.Vencimento).ToListAsync(ct))
    .WithSummary("Boletos, Pix e protocolos gerados");

api.MapGet("/auditoria", async (int? tarefaId, AgenteDbContext db, CancellationToken ct) =>
    {
        var eventos = await db.Eventos.AsNoTracking()
            .Where(e => tarefaId == null || e.TarefaId == tarefaId)
            .OrderByDescending(e => e.Em).ToListAsync(ct);
        return new { RiscoCidadao = eventos.Sum(e => e.Peso), Eventos = eventos };
    })
    .WithSummary("Eventos de segurança e indicador Risco Cidadão (soma ponderada: baixo 1, médio 3, alto 5)");

api.MapPost("/auditoria/eventos", (BloqueioExtensao b, TarefaService s, CancellationToken ct) => s.RegistrarBloqueioDaExtensaoAsync(b, ct))
    .WithSummary("A extensão registra que mascarou dados pessoais no navegador (taint analysis: fonte → sanitizador → sink)");

app.Run();

public partial class Program;
