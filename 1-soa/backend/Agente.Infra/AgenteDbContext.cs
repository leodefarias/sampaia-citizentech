using Agente.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Agente.Infra;

public class AgenteDbContext(DbContextOptions<AgenteDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<PassoTarefa> Passos => Set<PassoTarefa>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<EventoAuditoria> Eventos => Set<EventoAuditoria>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Enums gravados como texto: legível no Oracle e estável se a ordem mudar.
        b.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
        b.Properties<string>().HaveMaxLength(500);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Usuario>().ToTable("SAMPAIA_USUARIO");
        m.Entity<Servico>(e =>
        {
            e.ToTable("SAMPAIA_SERVICO");
            e.HasIndex(s => s.Chave).IsUnique();
        });
        m.Entity<Tarefa>(e =>
        {
            e.ToTable("SAMPAIA_TAREFA");
            e.HasOne(t => t.Servico).WithMany().HasForeignKey(t => t.ServicoId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId);
            e.HasMany(t => t.Passos).WithOne().HasForeignKey(p => p.TarefaId);
            e.HasMany(t => t.Documentos).WithOne().HasForeignKey(d => d.TarefaId);
            e.Ignore(t => t.Finalizada);
        });
        m.Entity<PassoTarefa>().ToTable("SAMPAIA_PASSO");
        m.Entity<Documento>(e =>
        {
            e.ToTable("SAMPAIA_DOCUMENTO");
            e.Property(d => d.Valor).HasPrecision(12, 2);
        });
        m.Entity<EventoAuditoria>(e =>
        {
            e.ToTable("SAMPAIA_EVENTO");
            e.HasOne<Tarefa>().WithMany().HasForeignKey(ev => ev.TarefaId);
            e.Ignore(ev => ev.Peso);
        });

        Seed(m);
    }

    private static void Seed(ModelBuilder m)
    {
        // Usuário simulado (regra da GS: nenhum dado pessoal real).
        m.Entity<Usuario>().HasData(new Usuario { Id = 1, Nome = "Dona Cida (simulada)", PreferenciasAcessibilidade = "fonte-grande" });
        m.Entity<Servico>().HasData(
            new Servico { Id = 1, Chave = "sabesp", Nome = "2ª via da conta de água", Orgao = "Sabesp", Modo = ModoServico.Login, UrlBase = "https://agenciavirtual.sabesp.com.br/", DominiosSensiveis = "novaagenciavirtual.sabesp.com.br/auth/login" },
            new Servico { Id = 2, Chave = "iptu", Nome = "Guia do IPTU", Orgao = "Prefeitura de São Paulo", Modo = ModoServico.Publico, UrlBase = "https://meuiptu.sf.prefeitura.sp.gov.br/servicos/pagamentos" },
            new Servico { Id = 3, Chave = "ipva", Nome = "Consulta e pagamento do IPVA", Orgao = "Sefaz-SP", Modo = ModoServico.Login, UrlBase = "https://www3.fazenda.sp.gov.br/SIVEI/DebitosVinculados/Consulta", DominiosSensiveis = "sso.acesso.gov.br" },
            new Servico { Id = 4, Chave = "sp156", Nome = "Pedido de zeladoria SP156", Orgao = "Prefeitura de São Paulo", Modo = ModoServico.Anonimo, UrlBase = "https://sp156.prefeitura.sp.gov.br/portal/servicos" },
            new Servico { Id = 5, Chave = "poupatempo", Nome = "Agendamento no Poupatempo (simulado)", Orgao = "Portal simulado", Modo = ModoServico.Login, UrlBase = "http://localhost:5173/", DominiosSensiveis = "127.0.0.1:5174" },
            new Servico { Id = 6, Chave = "poupatempo-sp", Nome = "Agendamento no Poupatempo", Orgao = "Poupatempo SP", Modo = ModoServico.Login, UrlBase = "https://www.poupatempo.sp.gov.br/", DominiosSensiveis = "sso.acesso.gov.br" });
    }
}

/// <summary>Usado só pelo `dotnet ef`. Migrações são geradas para Oracle, o banco de entrega.</summary>
public class AgenteDbContextFactory : IDesignTimeDbContextFactory<AgenteDbContext>
{
    public AgenteDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AgenteDbContext>()
            .UseOracle(Environment.GetEnvironmentVariable("ConnectionStrings__Oracle") ?? "User Id=sampaia;Password=x;Data Source=localhost:1521/FREEPDB1",
                o => o.UseOracleSQLCompatibility(Microsoft.EntityFrameworkCore.OracleSQLCompatibility.DatabaseVersion19))
            .Options);
}
