using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lembretes.Api;

public enum CanalLembrete { App, Esp32 }

public class Lembrete
{
    public int Id { get; set; }
    /// <summary>Documento da API do Agente que originou o lembrete (referência entre serviços, sem FK).</summary>
    public int? DocumentoId { get; set; }
    public required string Titulo { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly DataAviso { get; set; }
    public CanalLembrete Canal { get; set; } = CanalLembrete.App;
    public bool Concluido { get; set; }
}

public record NovoLembrete(string Titulo, DateOnly DataVencimento, int? DocumentoId = null, DateOnly? DataAviso = null, CanalLembrete Canal = CanalLembrete.App);

/// <summary>Banco próprio do serviço de Lembretes (SOA: cada serviço com seus dados).</summary>
public class LembretesDbContext(DbContextOptions<LembretesDbContext> options) : DbContext(options)
{
    public DbSet<Lembrete> Lembretes => Set<Lembrete>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<Enum>().HaveConversion<string>().HaveMaxLength(10);
        b.Properties<string>().HaveMaxLength(200);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Lembrete>(e =>
        {
            e.ToTable("SAMPAIA_LEMBRETE");
            e.HasIndex(l => new { l.Concluido, l.DataVencimento });
        });
        // Exemplos simulados para o app e o ESP32 terem o que mostrar na demo.
        m.Entity<Lembrete>().HasData(
            new Lembrete { Id = 1, Titulo = "Conta de água", DataVencimento = new(2026, 11, 20), DataAviso = new(2026, 11, 19), Canal = CanalLembrete.Esp32 },
            new Lembrete { Id = 2, Titulo = "IPTU parcela 11", DataVencimento = new(2026, 11, 25), DataAviso = new(2026, 11, 24) });
    }
}

public class LembretesDbContextFactory : IDesignTimeDbContextFactory<LembretesDbContext>
{
    public LembretesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LembretesDbContext>()
            .UseOracle(Environment.GetEnvironmentVariable("ConnectionStrings__Oracle") ?? "User Id=sampaia;Password=x;Data Source=localhost:1521/FREEPDB1",
                o => o.UseOracleSQLCompatibility(Microsoft.EntityFrameworkCore.OracleSQLCompatibility.DatabaseVersion19))
            .Options);
}
