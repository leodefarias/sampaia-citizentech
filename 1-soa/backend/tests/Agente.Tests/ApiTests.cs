using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Agente.Domain;
using Agente.Infra;
using Moq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Agente.Tests;

public class ApiTests : IClassFixture<ApiTests.Fabrica>
{
    public class Fabrica : WebApplicationFactory<Program>
    {
        private readonly string _banco = Path.Combine(Path.GetTempPath(), $"sampaia-teste-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder b)
        {
            b.UseSetting("ConnectionStrings:Sqlite", $"Data Source={_banco}");
            b.UseSetting("ConnectionStrings:Oracle", ""); // força SQLite mesmo com Oracle configurado na máquina
            b.UseSetting("ANTHROPIC_API_KEY", ""); // testes sempre usam o LLM fake
        }
    }

    private readonly HttpClient _http;
    public ApiTests(Fabrica f) => _http = f.CreateClient();

    private async Task<JsonElement> Post(string url, object corpo, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var r = await _http.PostAsJsonAsync(url, corpo);
        Assert.Equal(esperado, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>(Json.Opcoes);
    }

    private async Task<int> CriarTarefa(string intencao) =>
        (await Post("/api/tarefas", new { intencao })).GetProperty("tarefa").GetProperty("id").GetInt32();

    [Fact]
    public async Task Catalogo_tem_os_seis_servicos()
    {
        var servicos = await _http.GetFromJsonAsync<JsonElement>("/api/servicos");
        Assert.Equal(6, servicos.GetArrayLength());
    }

    [Fact]
    public async Task Pedido_vira_plano_do_servico_certo()
    {
        var r = await Post("/api/tarefas", new { intencao = "Oi, Paulinha, quero pagar meu IPTU" });
        Assert.Equal("iptu", r.GetProperty("tarefa").GetProperty("servico").GetProperty("chave").GetString());
        Assert.Equal("navegando", r.GetProperty("tarefa").GetProperty("estado").GetString());
        Assert.True(r.GetProperty("plano").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Pedido_sem_servico_explica_em_linguagem_simples()
    {
        var r = await Post("/api/tarefas", new { intencao = "bom dia" }, HttpStatusCode.BadRequest);
        Assert.Contains("Não entendi", r.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Cpf_no_chat_nao_e_gravado()
    {
        var cpf = Dados.Cpf("987654321");
        var r = await Post("/api/tarefas", new { intencao = $"pagar IPTU, meu CPF é {cpf}" });
        var intencao = r.GetProperty("tarefa").GetProperty("intencao").GetString()!;
        Assert.DoesNotContain(cpf, intencao);
        Assert.Contains("[CPF]", intencao);
    }

    [Fact]
    public async Task Cpf_na_pagina_gera_evento_de_auditoria()
    {
        var id = await CriarTarefa("pagar IPTU");
        var acao = await Post($"/api/tarefas/{id}/proximo-passo",
            new { url = "https://meuiptu.sf.prefeitura.sp.gov.br/", conteudo = $"CPF {Dados.Cpf("111444777")}" });
        Assert.Equal("navegar", acao.GetProperty("acao").GetString());

        var aud = await _http.GetFromJsonAsync<JsonElement>($"/api/auditoria?tarefaId={id}");
        Assert.Equal(3, aud.GetProperty("riscoCidadao").GetInt32());
        Assert.Equal("PiiSemMascaraDaExtensao", aud.GetProperty("eventos")[0].GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task Handoff_de_login_pausa_e_retoma()
    {
        var id = await CriarTarefa("quero agendar no poupatempo simulado");
        var acao = await Post($"/api/tarefas/{id}/proximo-passo", new { url = "https://sso.acesso.gov.br/login", conteudo = "Entrar" });
        Assert.Equal("aguardarUsuario", acao.GetProperty("acao").GetString());

        var t = await Post($"/api/tarefas/{id}/retomar", new { });
        Assert.Equal("navegando", t.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Recusar_confirmacao_encerra_sem_gerar_nada()
    {
        var id = await CriarTarefa("pagar IPTU");
        // Ainda navegando: não dá para confirmar.
        await Post($"/api/tarefas/{id}/confirmar", new { confirmado = true }, HttpStatusCode.Conflict);

        var t = await Post($"/api/tarefas/{id}/falhar", new { motivo = "Usuário cancelou" });
        Assert.Equal("falhou", t.GetProperty("estado").GetString());
        Assert.Equal(0, t.GetProperty("documentos").GetArrayLength());
    }

    [Fact]
    public async Task Tarefa_inexistente_da_404()
    {
        var r = await _http.GetAsync("/api/tarefas/99999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }
}

public class IntegracaoLembretesTests(ApiTests.Fabrica fabrica) : IClassFixture<ApiTests.Fabrica>
{
    [Fact]
    public async Task Boleto_com_vencimento_cria_lembrete_na_outra_api()
    {
        var lembretes = new Mock<ILembretesClient>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.ProximaAcaoAsync(It.IsAny<PedidoProximaAcao>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcaoAgente(TipoAcao.Concluir, null, null, false, "Pronto"));

        var http = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton(lembretes.Object);
            s.AddSingleton(llm.Object);
        })).CreateClient();

        var criada = await (await http.PostAsJsonAsync("/api/tarefas", new { intencao = "segunda via da conta de água" }))
            .Content.ReadFromJsonAsync<JsonElement>(Json.Opcoes);
        var id = criada.GetProperty("tarefa").GetProperty("id").GetInt32();

        await http.PostAsJsonAsync($"/api/tarefas/{id}/proximo-passo",
            new { url = "https://agenciavirtual.sabesp.com.br/", conteudo = "Total R$ 87,40 Vencimento: 20/11/2026" });

        var docs = await http.GetFromJsonAsync<JsonElement>("/api/documentos", Json.Opcoes);
        Assert.Contains(docs.EnumerateArray(), d => d.GetProperty("valor").GetDecimal() == 87.40m);
        lembretes.Verify(l => l.CriarAsync(
            It.Is<LembreteNovo>(n => n.DataVencimento == new DateOnly(2026, 11, 20)), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class PrevisaoTests(ApiTests.Fabrica fabrica) : IClassFixture<ApiTests.Fabrica>
{
    [Fact]
    public async Task Servico_de_ml_fora_do_ar_usa_estimativa()
    {
        // Aponta para uma porta sem nada: o cliente cai na estimativa em vez de quebrar o app.
        var http = fabrica.WithWebHostBuilder(b => b.UseSetting("Previsao:Url", "http://127.0.0.1:9")).CreateClient();
        var r = await http.PostAsJsonAsync("/api/previsao-prazo", new { tipoServico = "Árvore (poda ou queda)", distrito = "SÉ" });
        var p = await r.Content.ReadFromJsonAsync<JsonElement>(Json.Opcoes);
        Assert.Equal(("8 a 30 dias", "estimativa"), (p.GetProperty("faixa").GetString(), p.GetProperty("fonte").GetString()));
    }
}
