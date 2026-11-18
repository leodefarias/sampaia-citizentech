using System.Text.Json;
using System.Text.RegularExpressions;
using Agente.Domain;

namespace Agente.Infra;

/// <summary>Aplica as regras de security/regras-pii.json, em ordem. Mesmas regras usadas pela extensão.</summary>
public class PiiMasker : IPiiMasker
{
    private record Regra(string Nome, Regex Padrao, string? Marcador, string? Validador, bool Bloqueia);
    private record RegraJson(string Nome, string Padrao, string? Flags, string? Marcador, string? Validador, bool Bloqueia);
    private record ArquivoJson(List<RegraJson> Regras);

    public const string Bloqueado = "[BLOQUEADO]";
    private readonly List<Regra> _regras;

    public PiiMasker(string caminhoRegras)
    {
        var arquivo = JsonSerializer.Deserialize<ArquivoJson>(File.ReadAllText(caminhoRegras), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Arquivo de regras de PII vazio.");
        _regras = arquivo.Regras.Select(r => new Regra(
            r.Nome,
            new Regex(r.Padrao, (r.Flags?.Contains('i') == true ? RegexOptions.IgnoreCase : 0) | RegexOptions.Compiled, TimeSpan.FromSeconds(1)),
            r.Marcador, r.Validador, r.Bloqueia)).ToList();
    }

    /// <summary>Regras copiadas para a pasta de saída do build (ver Agente.Infra.csproj).</summary>
    public static PiiMasker Padrao() => new(Path.Combine(AppContext.BaseDirectory, "regras-pii.json"));

    public ResultadoMascara Mascarar(string texto)
    {
        var encontrados = new List<string>();
        foreach (var regra in _regras)
        {
            if (regra.Bloqueia)
            {
                if (regra.Padrao.IsMatch(texto)) return new ResultadoMascara(Bloqueado, true, [regra.Nome]);
                continue;
            }
            texto = regra.Padrao.Replace(texto, m =>
            {
                if (regra.Validador is { } v && !Validar(v, m.Value)) return m.Value;
                encontrados.Add(regra.Nome);
                return regra.Marcador!;
            });
        }
        return new ResultadoMascara(texto, false, encontrados);
    }

    private static bool Validar(string validador, string valor)
    {
        var d = valor.Where(char.IsDigit).Select(c => c - '0').ToArray();
        return validador switch
        {
            "cpf" => CpfValido(d),
            "cns" => CnsValido(d),
            "luhn" => LuhnValido(d),
            _ => throw new InvalidOperationException($"Validador desconhecido: {validador}")
        };
    }

    public static bool CpfValido(int[] d)
    {
        if (d.Length != 11 || d.All(x => x == d[0])) return false;
        for (var n = 9; n <= 10; n++)
        {
            var soma = 0;
            for (var i = 0; i < n; i++) soma += d[i] * (n + 1 - i);
            var dv = soma * 10 % 11 % 10;
            if (dv != d[n]) return false;
        }
        return true;
    }

    // CNS: soma ponderada (15..1) divisível por 11. Vale para cartões definitivos (1, 2) e provisórios (7, 8, 9).
    public static bool CnsValido(int[] d) =>
        d.Length == 15 && d.Select((x, i) => x * (15 - i)).Sum() % 11 == 0;

    public static bool LuhnValido(int[] d)
    {
        if (d.Length is < 13 or > 19) return false;
        var soma = 0;
        for (var i = 0; i < d.Length; i++)
        {
            var x = d[d.Length - 1 - i];
            if (i % 2 == 1) { x *= 2; if (x > 9) x -= 9; }
            soma += x;
        }
        return soma % 10 == 0;
    }
}
