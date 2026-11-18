using System.Net.Http.Json;
using Agente.Domain;
using Microsoft.Extensions.Logging;

namespace Agente.Infra;

/// <summary>Chama a API de Lembretes por HTTP. Falha aqui não derruba a tarefa: o documento já foi salvo.</summary>
public class LembretesClient(HttpClient http, ILogger<LembretesClient> log) : ILembretesClient
{
    public async Task CriarAsync(LembreteNovo lembrete, CancellationToken ct)
    {
        try
        {
            var r = await http.PostAsJsonAsync("/api/lembretes", lembrete, Json.Opcoes, ct);
            r.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            log.LogWarning("API de Lembretes indisponível ({Status}); lembrete do documento {Doc} não criado.", e.StatusCode, lembrete.DocumentoId);
        }
    }
}
