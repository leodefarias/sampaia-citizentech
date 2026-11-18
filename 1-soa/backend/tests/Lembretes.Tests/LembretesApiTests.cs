using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lembretes.Tests;

/// <summary>Relógio parado em 04/11/2026 para os testes de data não dependerem do dia.</summary>
public class RelogioFixo() : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public class LembretesApiTests : IClassFixture<LembretesApiTests.Fabrica>
{
    public class Fabrica : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder b)
        {
            b.UseSetting("ConnectionStrings:Sqlite", $"Data Source={Path.Combine(Path.GetTempPath(), $"lembretes-{Guid.NewGuid()}.db")}");
            b.UseSetting("ConnectionStrings:Oracle", "");
            b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(new RelogioFixo()));
        }
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    public LembretesApiTests(Fabrica f) => _http = f.CreateClient();

    private async Task<JsonElement> Criar(string titulo, string vencimento)
    {
        var r = await _http.PostAsJsonAsync("/api/lembretes", new { titulo, dataVencimento = vencimento });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>(Web);
    }

    [Fact]
    public async Task Aviso_padrao_e_na_vespera()
    {
        var l = await Criar("Conta de luz", "2026-12-10");
        Assert.Equal("2026-12-09", l.GetProperty("dataAviso").GetString());
    }

    [Fact]
    public async Task Proximo_traz_o_vencimento_mais_perto()
    {
        await Criar("Amanhã", "2026-11-05");
        var p = await _http.GetFromJsonAsync<JsonElement>("/api/lembretes/proximo", Web);
        Assert.Equal("Amanhã", p.GetProperty("titulo").GetString());
        Assert.Equal(1, p.GetProperty("dias").GetInt32());
        Assert.True(p.GetProperty("alerta").GetBoolean()); // véspera: toca o buzzer
    }

    [Fact]
    public async Task Filtro_de_proximos_dias()
    {
        await Criar("Longe", "2027-03-01");
        var lista = await _http.GetFromJsonAsync<JsonElement>("/api/lembretes?dias=7", Web);
        Assert.DoesNotContain(lista.EnumerateArray(), l => l.GetProperty("titulo").GetString() == "Longe");
    }

    [Fact]
    public async Task Concluir_tira_da_lista()
    {
        var id = (await Criar("Para pagar", "2026-11-30")).GetProperty("id").GetInt32();
        var r = await _http.PostAsync($"/api/lembretes/{id}/concluir", null);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var lista = await _http.GetFromJsonAsync<JsonElement>("/api/lembretes", Web);
        Assert.DoesNotContain(lista.EnumerateArray(), l => l.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Concluir_inexistente_da_404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _http.PostAsync("/api/lembretes/9999/concluir", null)).StatusCode);

    [Fact]
    public async Task Titulo_vazio_e_recusado()
    {
        var r = await _http.PostAsJsonAsync("/api/lembretes", new { titulo = "", dataVencimento = "2026-12-01" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}
