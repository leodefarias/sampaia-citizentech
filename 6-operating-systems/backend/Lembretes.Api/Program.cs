using System.Text.Json;
using System.Text.Json.Serialization;
using Lembretes.Api;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var oracle = builder.Configuration.GetConnectionString("Oracle");
builder.Services.AddDbContext<LembretesDbContext>(o =>
{
    if (!string.IsNullOrWhiteSpace(oracle)) o.UseOracle(oracle, x => x.UseOracleSQLCompatibility(Microsoft.EntityFrameworkCore.OracleSQLCompatibility.DatabaseVersion19));
    else o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=lembretes.db");
});
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddSingleton(TimeProvider.System); // substituível nos testes
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LembretesDbContext>();
    if (db.Database.IsSqlite()) db.Database.EnsureCreated();
}

app.UseCors();
app.MapOpenApi();
app.MapScalarApiReference();

var api = app.MapGroup("/api/lembretes");

api.MapGet("/", (int? dias, LembretesDbContext db, TimeProvider relogio, CancellationToken ct) =>
    {
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        return db.Lembretes.AsNoTracking()
            .Where(l => !l.Concluido && (dias == null || l.DataVencimento <= hoje.AddDays(dias.Value)))
            .OrderBy(l => l.DataVencimento)
            .ToListAsync(ct);
    })
    .WithSummary("Lembretes em aberto; ?dias=7 traz só os que vencem nos próximos 7 dias");

api.MapPost("/", async (NovoLembrete n, LembretesDbContext db, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(n.Titulo)) return Results.Problem("Título é obrigatório.", statusCode: 400);
        var l = new Lembrete
        {
            Titulo = n.Titulo, DataVencimento = n.DataVencimento, DocumentoId = n.DocumentoId, Canal = n.Canal,
            DataAviso = n.DataAviso ?? n.DataVencimento.AddDays(-1), // aviso padrão: véspera
        };
        db.Lembretes.Add(l);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/lembretes/{l.Id}", l);
    })
    .WithSummary("Cria um lembrete (a API do Agente chama quando gera um documento com vencimento)");

api.MapGet("/proximo", async (LembretesDbContext db, TimeProvider relogio, CancellationToken ct) =>
    {
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        var l = await db.Lembretes.AsNoTracking()
            .Where(x => !x.Concluido && x.DataVencimento >= hoje)
            .OrderBy(x => x.DataVencimento).FirstOrDefaultAsync(ct);
        if (l is null) return Results.NoContent();
        var dias = l.DataVencimento.DayNumber - hoje.DayNumber;
        // Formato enxuto para o display do ESP32.
        return Results.Ok(new { l.Id, l.Titulo, l.DataVencimento, Dias = dias, Alerta = hoje >= l.DataAviso });
    })
    .WithSummary("Próximo vencimento, para o ESP32 (204 se não houver)");

api.MapPost("/{id:int}/concluir", async (int id, LembretesDbContext db, CancellationToken ct) =>
    {
        var l = await db.Lembretes.FindAsync([id], ct);
        if (l is null) return Results.Problem("Lembrete não encontrado.", statusCode: 404);
        l.Concluido = true;
        await db.SaveChangesAsync(ct);
        return Results.Ok(l);
    })
    .WithSummary("Marca como pago (botão \"Já paguei\")");

app.Run();

public partial class Program;
