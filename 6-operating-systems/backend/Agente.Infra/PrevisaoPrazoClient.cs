using System.Net.Http.Json;
using Agente.Domain;
using Microsoft.Extensions.Logging;

namespace Agente.Infra;

/// <summary>Chama o serviço de ML. Se ele estiver fora, devolve uma estimativa pela mediana de cada tipo.</summary>
public class PrevisaoPrazoClient(HttpClient http, ILogger<PrevisaoPrazoClient> log) : IPrevisaoPrazoClient
{
    // Faixa mais comum por tipo nos dados do SP156 (3º tri 2025 a 2º tri 2026), ver ml/dados/README.md.
    private static readonly Dictionary<string, string> Estimativa = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Buraco na rua"] = "ate7", ["Entulho ou lixo na rua"] = "ate7", ["Árvore (poda ou queda)"] = "8a30",
        ["Bueiro entupido"] = "ate7", ["Falta de varrição"] = "ate7",
    };
    private static readonly Dictionary<string, string> Texto = new() { ["ate7"] = "até 7 dias", ["8a30"] = "8 a 30 dias", ["mais30"] = "mais de 30 dias" };

    public async Task<PrevisaoPrazo> PreverAsync(PedidoPrevisao pedido, CancellationToken ct)
    {
        try
        {
            var r = await http.PostAsJsonAsync("/prever-prazo", pedido, Json.Opcoes, ct);
            r.EnsureSuccessStatusCode();
            return (await r.Content.ReadFromJsonAsync<PrevisaoPrazo>(Json.Opcoes, ct))!;
        }
        catch (HttpRequestException e)
        {
            log.LogWarning("Serviço de previsão indisponível ({Status}); usando estimativa.", e.StatusCode);
            var classe = Estimativa.GetValueOrDefault(pedido.TipoServico, "8a30");
            return new PrevisaoPrazo(Texto[classe], classe, "estimativa");
        }
    }
}
