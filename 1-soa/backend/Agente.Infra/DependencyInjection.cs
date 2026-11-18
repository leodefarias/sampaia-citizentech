using Agente.Domain;
using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Agente.Infra;

public static class DependencyInjection
{
    /// <param name="oracle">Connection string do Oracle; vazia = SQLite local.</param>
    /// <param name="sqlite">Connection string do SQLite (dev e testes).</param>
    /// <param name="usarClaude">true quando ANTHROPIC_API_KEY está definida.</param>
    /// <param name="urlLembretes">Endereço da API de Lembretes.</param>
    /// <param name="urlPrevisao">Endereço do serviço Python de previsão de prazo.</param>
    public static IServiceCollection AddInfra(this IServiceCollection s, string? oracle, string sqlite, bool usarClaude, string urlLembretes, string urlPrevisao)
    {
        s.AddDbContext<AgenteDbContext>(o =>
        {
            if (!string.IsNullOrWhiteSpace(oracle)) o.UseOracle(oracle, x => x.UseOracleSQLCompatibility(Microsoft.EntityFrameworkCore.OracleSQLCompatibility.DatabaseVersion19));
            else o.UseSqlite(sqlite);
        });

        s.AddSingleton<IPiiMasker>(_ => PiiMasker.Padrao());

        // Chave só no servidor, em variável de ambiente. Sem chave = LLM fake (demo offline e testes).
        if (usarClaude)
        {
            s.AddSingleton(new AnthropicClient());
            s.AddSingleton<ILlmClient, ClaudeLlmClient>();
        }
        else s.AddSingleton<ILlmClient, LlmFake>();

        s.AddHttpClient<ILembretesClient, LembretesClient>(c => { c.BaseAddress = new Uri(urlLembretes); c.Timeout = TimeSpan.FromSeconds(5); });

        s.AddHttpClient<IPrevisaoPrazoClient, PrevisaoPrazoClient>(c => { c.BaseAddress = new Uri(urlPrevisao); c.Timeout = TimeSpan.FromSeconds(5); });

        s.AddScoped<IConector, SabespConector>();
        s.AddScoped<IConector, IptuConector>();
        s.AddScoped<IConector, IpvaConector>();
        s.AddScoped<IConector, Sp156Conector>();
        // O simulado vem antes: "Poupatempo simulado" também contém "Poupatempo" (a fábrica pega o primeiro que atende).
        s.AddScoped<IConector, PoupatempoSimuladoConector>();
        s.AddScoped<IConector, PoupatempoConector>();
        s.AddScoped<IConectorFactory, ConectorFactory>();
        return s;
    }
}
