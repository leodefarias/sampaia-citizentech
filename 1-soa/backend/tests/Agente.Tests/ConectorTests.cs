using Agente.Domain;
using Agente.Infra;
using Moq;

namespace Agente.Tests;

public class ConectorTests
{
    private readonly Mock<ILlmClient> _llm = new();
    private readonly IPiiMasker _masker = PiiMasker.Padrao();

    private ConectorFactory Fabrica() => new IConector[]
    {
        new SabespConector(_llm.Object, _masker), new IptuConector(_llm.Object, _masker),
        new IpvaConector(_llm.Object, _masker), new Sp156Conector(_llm.Object, _masker),
        new PoupatempoSimuladoConector(_llm.Object, _masker), new PoupatempoConector(_llm.Object, _masker),
    } is var c ? new ConectorFactory(c) : null!;

    [Theory]
    [InlineData("Oi, Paulinha, quero pagar meu IPTU", "iptu")]
    [InlineData("preciso da segunda via da conta de água", "sabesp")]
    [InlineData("quanto é o IPVA do meu carro?", "ipva")]
    [InlineData("tem um buraco na minha rua", "sp156")]
    [InlineData("quero agendar o RG", "poupatempo-sp")]
    [InlineData("Quero agendar no Poupatempo", "poupatempo-sp")]
    [InlineData("quero agendar no Poupatempo simulado", "poupatempo")]
    public void Escolhe_conector_pela_intencao(string intencao, string chave) =>
        Assert.Equal(chave, Fabrica().PorIntencao(intencao)?.Chave);

    [Fact]
    public void Palavra_dentro_de_outra_nao_conta() =>
        Assert.Null(Fabrica().PorIntencao("é urgente, aguardo"));

    [Fact]
    public async Task Llm_recebe_so_texto_mascarado()
    {
        PedidoProximaAcao? enviado = null;
        _llm.Setup(l => l.ProximaAcaoAsync(It.IsAny<PedidoProximaAcao>(), It.IsAny<CancellationToken>()))
            .Callback<PedidoProximaAcao, CancellationToken>((p, _) => enviado = p)
            .ReturnsAsync(new AcaoAgente(TipoAcao.Preencher, "#cpf", Dados.Cpf(), false, "Vou preencher"));

        var cpf = Dados.Cpf();
        var acao = await new IptuConector(_llm.Object, _masker).ProximoPassoAsync(
            new Tarefa { Intencao = "pagar IPTU" },
            new RetratoPagina("https://meuiptu.sf.prefeitura.sp.gov.br/", "IPTU", $"Contribuinte CPF {cpf}"),
            CancellationToken.None);

        Assert.DoesNotContain(cpf, enviado!.ConteudoMascarado);
        Assert.Contains("[CPF]", enviado.ConteudoMascarado);
        Assert.Equal("[CPF]", acao.Valor); // valor devolvido pelo LLM também é mascarado
    }

    [Fact]
    public async Task Login_gov_br_nao_chama_llm()
    {
        var acao = await new PoupatempoSimuladoConector(_llm.Object, _masker).ProximoPassoAsync(
            new Tarefa { Intencao = "agendar" },
            new RetratoPagina("https://sso.acesso.gov.br/login", null, "Entre com sua conta"),
            CancellationToken.None);

        Assert.Equal(TipoAcao.AguardarUsuario, acao.Acao);
        _llm.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Pagina_com_senha_nao_chama_llm()
    {
        var acao = await new IptuConector(_llm.Object, _masker).ProximoPassoAsync(
            new Tarefa { Intencao = "pagar IPTU" },
            new RetratoPagina("https://meuiptu.sf.prefeitura.sp.gov.br/", null, "senha: 1234"),
            CancellationToken.None);

        Assert.Equal(TipoAcao.AguardarUsuario, acao.Acao);
        _llm.VerifyNoOtherCalls();
    }
}

public class PoupatempoSimuladoTests
{
    private readonly PoupatempoSimuladoConector _c = new(new Mock<ILlmClient>(MockBehavior.Strict).Object, PiiMasker.Padrao());
    private readonly Tarefa _t = new() { Intencao = "agendar", Servico = new Servico { Chave = "poupatempo", Nome = "x", Orgao = "x", UrlBase = "http://localhost:5173/" } };

    private Task<AcaoAgente> Passo(string url, string conteudo) =>
        _c.ProximoPassoAsync(_t, new RetratoPagina(url, null, conteudo), CancellationToken.None);

    [Fact]
    public async Task Fora_do_portal_navega() =>
        Assert.Equal(TipoAcao.Navegar, (await Passo("about:blank", "")).Acao);

    [Fact]
    public async Task Preenche_campos_vazios_em_ordem()
    {
        const string vazio = "#posto (select) \"Posto\" = \"\"\n#data (input:date) \"Dia\" = \"\"\n#horario (select) \"Horário\" = \"\"";
        var a = await Passo("http://localhost:5173/agendar.html", vazio);
        Assert.Equal(("#posto", PoupatempoSimuladoConector.PostoPadrao), (a.Seletor, a.Valor));

        var b = await Passo("http://localhost:5173/agendar.html", vazio.Replace("\"Posto\" = \"\"", "\"Posto\" = \"Poupatempo Sé\""));
        Assert.Equal("#data", b.Seletor);
    }

    [Fact]
    public async Task Resumo_pede_confirmacao()
    {
        var a = await Passo("http://localhost:5173/agendar.html", "#btn-confirmar (button) \"Confirmar agendamento\" = \"\"");
        Assert.Equal(TipoAcao.Confirmar, a.Acao);
        Assert.True(a.ExigeConfirmacao);
    }

    [Fact]
    public async Task Comprovante_conclui_e_vira_documento()
    {
        var r = new RetratoPagina("http://localhost:5173/agendar.html", null, "Protocolo: PT-2026-123456 Data do atendimento: 11/11/2026 às 09:00");
        Assert.Equal(TipoAcao.Concluir, (await _c.ProximoPassoAsync(_t, r, CancellationToken.None)).Acao);
        var doc = await _c.ExtrairDocumentoAsync(_t, r, CancellationToken.None);
        Assert.Equal((TipoDocumento.Agendamento, new DateOnly(2026, 11, 11), "PT-2026-123456"), (doc!.Tipo, doc.Vencimento, doc.Codigo));
    }
}

public class DeteccaoLoginTests
{
    [Theory]
    [InlineData("https://sso.acesso.gov.br/login?client_id=x", true)]
    [InlineData("https://novaagenciavirtual.sabesp.com.br/auth/login", true)]
    [InlineData("http://127.0.0.1:5174/login.html", true)]
    [InlineData("https://novaagenciavirtual.sabesp.com.br/contas", false)]
    [InlineData("https://acesso.gov.br.golpe.com/login", false)]
    [InlineData("http://localhost:5173/agendar.html", false)]
    public void Reconhece_telas_de_login(string url, bool esperado) => Assert.Equal(esperado, ConectorBase.EhLogin(url));
}
