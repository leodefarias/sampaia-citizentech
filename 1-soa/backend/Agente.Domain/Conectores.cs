using System.Globalization;
using System.Text.RegularExpressions;

namespace Agente.Domain;

public interface IConector
{
    string Chave { get; }
    bool Atende(string intencao);
    Task<IReadOnlyList<string>> MontarPlanoAsync(Tarefa tarefa, CancellationToken ct);
    Task<AcaoAgente> ProximoPassoAsync(Tarefa tarefa, RetratoPagina retrato, CancellationToken ct);
    Task<Documento?> ExtrairDocumentoAsync(Tarefa tarefa, RetratoPagina retrato, CancellationToken ct);
}

public abstract class ConectorBase(ILlmClient llm, IPiiMasker masker) : IConector
{
    public abstract string Chave { get; }
    protected abstract string[] PalavrasChave { get; }
    protected abstract string[] Plano { get; }
    /// <summary>Dicas específicas do site para o LLM (seletores conhecidos, armadilhas).</summary>
    protected virtual string Instrucoes => "";

    public bool Atende(string intencao) =>
        PalavrasChave.Any(p => Regex.IsMatch(intencao, $@"\b{Regex.Escape(p)}\b", RegexOptions.IgnoreCase));

    public Task<IReadOnlyList<string>> MontarPlanoAsync(Tarefa tarefa, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Plano);

    public virtual async Task<AcaoAgente> ProximoPassoAsync(Tarefa tarefa, RetratoPagina retrato, CancellationToken ct)
    {
        if (EhLogin(retrato.Url))
            return AcaoAgente.EsperarUsuario("Agora é com você: entre na sua conta. Eu espero aqui e continuo depois.");

        // Sink: requisição ao LLM. Mascara de novo mesmo que a extensão já tenha mascarado.
        var seguro = masker.Mascarar(retrato.Conteudo);
        if (seguro.Bloqueado)
            return AcaoAgente.EsperarUsuario("Essa tela pede senha ou código. Digite você mesmo; eu não leio senhas.");

        var pedido = new PedidoProximaAcao(
            tarefa.Servico?.Nome ?? Chave, tarefa.Servico?.UrlBase ?? "", tarefa.Intencao, Plano,
            tarefa.Passos.Select(p => $"{p.Acao}: {p.Resultado}").ToList(), Instrucoes,
            masker.Mascarar(retrato.Url).Texto, seguro.Texto);

        var acao = await llm.ProximaAcaoAsync(pedido, ct);
        // O LLM nunca deve devolver dado pessoal no valor; se devolver, vira marcador.
        return acao with { Valor = acao.Valor is null ? null : masker.Mascarar(acao.Valor).Texto };
    }

    /// <summary>Tipo do documento que o serviço gera; null = não gera documento.</summary>
    protected virtual TipoDocumento? DocumentoGerado => null;

    /// <summary>Lê valor e data da página final (texto já mascarado: datas e valores não são dado pessoal).</summary>
    public virtual Task<Documento?> ExtrairDocumentoAsync(Tarefa tarefa, RetratoPagina retrato, CancellationToken ct)
    {
        if (DocumentoGerado is not { } tipo) return Task.FromResult<Documento?>(null);
        var valor = Regex.Match(retrato.Conteudo, @"R\$\s*([\d.]+,\d{2})");
        var data = Regex.Match(retrato.Conteudo, @"(?:venc\w*|data|dia)\D{0,20}(\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase);
        return Task.FromResult<Documento?>(new Documento
        {
            TarefaId = tarefa.Id,
            Tipo = tipo,
            Valor = valor.Success ? decimal.Parse(valor.Groups[1].Value.Replace(".", "").Replace(',', '.'), CultureInfo.InvariantCulture) : null,
            Vencimento = data.Success ? DateOnly.ParseExact(data.Groups[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture) : null,
            Codigo = Regex.Match(retrato.Conteudo, @"Protocolo:?\s*([A-Z0-9][A-Z0-9-]{3,})", RegexOptions.IgnoreCase) is { Success: true } p ? p.Groups[1].Value : null,
        });
    }

    /// <summary>
    /// Telas de login: o agente pausa e o usuário entra sozinho. Mesma lista da extensão (lib/config.ts).
    /// gov.br e o login simulado ficam fora das permissões da extensão; o login da Sabesp fica dentro
    /// do host liberado, então ali a garantia é do código (pausa + campo de senha nunca lido).
    /// </summary>
    public static bool EhLogin(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
        return u.Host == "acesso.gov.br" || u.Host.EndsWith(".acesso.gov.br", StringComparison.OrdinalIgnoreCase)
            || u.Authority == "127.0.0.1:5174"
            || (u.Host == "novaagenciavirtual.sabesp.com.br" && u.AbsolutePath.StartsWith("/auth/login", StringComparison.OrdinalIgnoreCase));
    }
}

public class SabespConector(ILlmClient llm, IPiiMasker masker) : ConectorBase(llm, masker)
{
    protected override TipoDocumento? DocumentoGerado => TipoDocumento.Boleto;
    public override string Chave => "sabesp";
    protected override string[] PalavrasChave => ["sabesp", "água", "agua", "esgoto"];
    protected override string[] Plano =>
        ["Abrir a Agência Virtual da Sabesp", "Você entra com CPF e senha da Sabesp", "Abrir as contas em aberto", "Gerar a 2ª via da conta"];
    protected override string Instrucoes => """
        Mapeado em 04/11/2026: agenciavirtual.sabesp.com.br redireciona para novaagenciavirtual.sabesp.com.br/auth/login
        (campos #cpf e #password). Nunca preencha login: use aguardarUsuario. Depois do login, procure "2ª via" ou "Contas",
        escolha a conta em aberto mais antiga e gere a 2ª via (confirmar antes). Aviso de cookies: #onetrust-reject-all-handler.
        """;
}

public class IptuConector(ILlmClient llm, IPiiMasker masker) : ConectorBase(llm, masker)
{
    protected override TipoDocumento? DocumentoGerado => TipoDocumento.Boleto;
    public override string Chave => "iptu";
    protected override string[] PalavrasChave => ["iptu", "imóvel", "imovel"];
    protected override string[] Plano =>
        ["Abrir a página de pagamentos do IPTU", "Entrar no sistema de emissão", "Informar o número SQL do imóvel (está no carnê)", "Escolher a parcela do mês", "Gerar a guia de pagamento"];
    protected override string Instrucoes => """
        Mapeado em 04/11/2026: em meuiptu.sf.prefeitura.sp.gov.br/servicos/pagamentos o link "Acessar o Serviço" leva a
        iptu.prefeitura.sp.gov.br (sistema de emissão). Preencha o SQL sempre com o marcador [SQL]. Escolha a parcela do
        mês atual. Emitir a guia exige confirmar. SQL inválido: explique em linguagem simples e use aguardarUsuario.
        Aviso de cookies: botão "REJEITAR COOKIES".
        """;
}

public class IpvaConector(ILlmClient llm, IPiiMasker masker) : ConectorBase(llm, masker)
{
    protected override TipoDocumento? DocumentoGerado => TipoDocumento.Pix;
    public override string Chave => "ipva";
    protected override string[] PalavrasChave => ["ipva", "carro", "veículo", "veiculo", "moto"];
    protected override string[] Plano =>
        ["Abrir a consulta de débitos do veículo (Sefaz-SP)", "Você entra com o gov.br", "Informar o Renavam", "Você resolve o captcha, se aparecer", "Mostrar o QR code Pix de pagamento"];
    protected override string Instrucoes => """
        Mapeado em 04/11/2026: www3.fazenda.sp.gov.br/SIVEI/DebitosVinculados/Consulta pede autenticação. Escolha
        pessoa física e clique em "Entrar com gov.br" (#loginPessoaFisicaLoginUnicoFederal); o login é com o usuário.
        Depois informe o Renavam com o marcador [RENAVAM]. Captcha: aguardarUsuario, nunca tente resolver.
        Gerar guia ou QR code Pix exige confirmar.
        """;
}

public class Sp156Conector(ILlmClient llm, IPiiMasker masker) : ConectorBase(llm, masker)
{
    protected override TipoDocumento? DocumentoGerado => TipoDocumento.Protocolo;
    public override string Chave => "sp156";
    protected override string[] PalavrasChave => ["156", "buraco", "lâmpada", "lampada", "iluminação", "iluminacao", "lixo", "árvore", "arvore", "calçada", "calcada"];
    protected override string[] Plano =>
        ["Abrir o portal SP156", "Escolher o tipo de pedido", "Informar o local", "Enviar e guardar o protocolo"];
    protected override string Instrucoes => """
        Mapeado em 04/11/2026: sp156.prefeitura.sp.gov.br/portal/servicos. Feche o comunicado inicial (#comunicado__ok).
        Use a busca ou os temas (ex.: "Lixo e limpeza", "Ruas e calçadas") para achar o serviço. Prefira solicitação
        anônima quando o serviço permitir; se pedir login, use aguardarUsuario. Enviar o pedido exige confirmar.
        """;
}

/// <summary>
/// Portal simulado da equipe. Roteiro determinístico (não usa LLM): a demo do handoff de login
/// e da confirmação funciona sempre, mesmo offline.
/// </summary>
/// <summary>Poupatempo de verdade: o LLM navega guiado pelo mapeamento abaixo. O login é sempre o gov.br, feito pela pessoa.</summary>
public class PoupatempoConector(ILlmClient llm, IPiiMasker masker) : ConectorBase(llm, masker)
{
    protected override TipoDocumento? DocumentoGerado => TipoDocumento.Agendamento;
    public override string Chave => "poupatempo-sp";
    protected override string[] PalavrasChave => ["poupatempo", "agendar", "agendamento", "rg", "carteira de identidade", "cin"];
    protected override string[] Plano =>
        ["Abrir o site do Poupatempo", "Achar o serviço que você precisa", "Você entra com o gov.br", "Você escolhe o posto; eu escolho o primeiro horário livre", "Confirmar o agendamento"];
    protected override string Instrucoes => """
        Mapeado em 05/11/2026. Comece em www.poupatempo.sp.gov.br. Ache o serviço do pedido (busca, "Serviços por assunto"
        ou os mais procurados, ex.: "Obter 2ª via de Carteira de Identidade Nacional - CIN") e abra a página dele (/carta/...).
        Nela clique em "Presencial" ou "Iniciar"/"agendar": o agendamento abre em operacoes.sp.gov.br/OperacoesWeb.
        Se pedir login, clique em "Entrar com o gov.br" (a pessoa entra sozinha). No agendamento escolha "Agendar para mim".
        O posto é escolha da pessoa: acao = aguardarUsuario pedindo que ela escolha o posto na página.
        Depois escolha a primeira data e o primeiro horário livres: no calendário, os dias com vaga aparecem nos
        ELEMENTOS marcados "[dia com vaga]"; clique no primeiro deles (os outros dias não têm vaga). Antes do botão final que confirma o agendamento:
        acao = confirmar. Quando aparecer o comprovante/protocolo: acao = concluir.
        """;
}

public class PoupatempoSimuladoConector(ILlmClient llm, IPiiMasker masker) : ConectorBase(llm, masker)
{
    public const string PostoPadrao = "Poupatempo Sé";
    public const string HorarioPadrao = "09:00";

    protected override TipoDocumento? DocumentoGerado => TipoDocumento.Agendamento;
    public override string Chave => "poupatempo";
    // Só quando a pessoa pede o simulado (demo e testes); "Poupatempo" sozinho vai para o site real.
    protected override string[] PalavrasChave => ["simulado", "portal simulado"];
    protected override string[] Plano =>
        ["Abrir o portal simulado", "Você entra com a sua conta (login simulado)", "Escolher posto, data e horário", "Confirmar o agendamento"];

    public override async Task<AcaoAgente> ProximoPassoAsync(Tarefa tarefa, RetratoPagina retrato, CancellationToken ct)
    {
        if (EhLogin(retrato.Url)) return await base.ProximoPassoAsync(tarefa, retrato, ct);

        var urlBase = tarefa.Servico?.UrlBase ?? "http://localhost:5173/";
        if (!retrato.Url.StartsWith(urlBase, StringComparison.OrdinalIgnoreCase))
            return new AcaoAgente(TipoAcao.Navegar, null, urlBase, false, "Vou abrir o portal de agendamento.");

        var c = retrato.Conteudo;
        if (c.Contains("Protocolo:"))
            return new AcaoAgente(TipoAcao.Concluir, null, null, false, "Pronto! Seu agendamento está confirmado.");
        if (Tem(c, "#btn-confirmar"))
            return new AcaoAgente(TipoAcao.Confirmar, "#btn-confirmar", null, true, "Vou confirmar o agendamento com os dados que aparecem na tela.");
        if (Tem(c, "#posto"))
        {
            if (Vazio(c, "#posto")) return Preencher("#posto", PostoPadrao, $"Vou escolher o posto {PostoPadrao}, no centro.");
            if (Vazio(c, "#data")) return Preencher("#data", ProximoDiaUtil().ToString("yyyy-MM-dd"), "Vou escolher o primeiro dia útil da semana que vem.");
            if (Vazio(c, "#horario")) return Preencher("#horario", HorarioPadrao, $"Vou escolher o horário das {HorarioPadrao}.");
            return new AcaoAgente(TipoAcao.Clicar, "#btn-continuar", null, false, "Vou continuar para conferir.");
        }
        if (Tem(c, "#btn-agendar"))
            return new AcaoAgente(TipoAcao.Clicar, "#btn-agendar", null, false, "Vou abrir o agendamento.");
        return AcaoAgente.EsperarUsuario("Não reconheci esta página. Pode voltar para o portal?");
    }

    private static bool Tem(string c, string seletor) => c.Contains(seletor + " (");
    private static bool Vazio(string c, string seletor) => Regex.IsMatch(c, Regex.Escape(seletor) + @" \([^)]*\) ""[^""]*"" = """"");
    private static AcaoAgente Preencher(string seletor, string valor, string explicacao) => new(TipoAcao.Preencher, seletor, valor, false, explicacao);

    private static DateOnly ProximoDiaUtil()
    {
        var d = DateOnly.FromDateTime(DateTime.Today).AddDays(7);
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        return d;
    }
}

public interface IConectorFactory
{
    IConector? PorChave(string chave);
    IConector? PorIntencao(string intencao);
}

/// <summary>Strategy: escolhe o conector certo para o serviço ou para o pedido em texto livre.</summary>
public class ConectorFactory(IEnumerable<IConector> conectores) : IConectorFactory
{
    public IConector? PorChave(string chave) => conectores.FirstOrDefault(c => c.Chave == chave);
    public IConector? PorIntencao(string intencao) => conectores.FirstOrDefault(c => c.Atende(intencao));
}
