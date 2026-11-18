namespace Agente.Tests;

/// <summary>Gera números sintéticos válidos. Nada de dado pessoal real em teste (regra da GS).</summary>
public static class Dados
{
    public static string Cpf(string nove = "123456789")
    {
        var d = nove.Select(c => c - '0').ToList();
        for (var n = 9; n <= 10; n++)
            d.Add(d.Select((x, i) => x * (n + 1 - i)).Sum() * 10 % 11 % 10);
        var s = string.Concat(d);
        return $"{s[..3]}.{s[3..6]}.{s[6..9]}-{s[9..]}";
    }

    /// <summary>CNS provisório (começa com 7) com soma ponderada divisível por 11.</summary>
    public static string Cns()
    {
        for (long b = 70000000000000; ; b++)
        {
            var d = b.ToString().Select(c => c - '0').ToList();
            var dv = (11 - d.Select((x, i) => x * (15 - i)).Sum() % 11) % 11;
            if (dv <= 9) return string.Concat(d) + dv;
        }
    }
}
