using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Agente.Domain;
using Agente.Infra;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agente.Tests;

/// <summary>Evidências da taint analysis: CPF injetado nunca chega a log, banco ou LLM.</summary>
public class SegurancaTests(ApiTests.Fabrica fabrica) : IClassFixture<ApiTests.Fabrica>
{
    /// <summary>Guarda toda mensagem de log da API para conferir que nenhuma tem o CPF.</summary>
    private sealed class LogEmMemoria : ILoggerProvider, ILogger
    {
        public readonly ConcurrentQueue<string> Linhas = new();
        public ILogger CreateLogger(string categoria) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel nivel) => true;
        public void Log<TState>(LogLevel nivel, EventId id, TState estado, Exception? erro, Func<TState, Exception?, string> formatar) =>
            Linhas.Enqueue(formatar(estado, erro) + erro);
        public void Dispose() { }
    }

    [Fact]
    public async Task Cpf_injetado_na_pagina_nao_aparece_em_log_banco_nem_llm()
    {
        var cpf = Dados.Cpf("246813579");
        var logs = new LogEmMemoria();
        var enviados = new ConcurrentQueue<string>();
        var llm = new Mock<ILlmClient>();
        llm.Setup(l => l.ProximaAcaoAsync(It.IsAny<PedidoProximaAcao>(), It.IsAny<CancellationToken>()))
            .Callback<PedidoProximaAcao, CancellationToken>((p, _) => enviados.Enqueue(JsonSerializer.Serialize(p)))
            .ReturnsAsync(new AcaoAgente(TipoAcao.Clicar, "#ok", null, false, "Vou continuar"));

        var http = fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton(llm.Object);
            s.AddLogging(l => l.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));
        })).CreateClient();

        var criada = await (await http.PostAsJsonAsync("/api/tarefas", new { intencao = $"pagar IPTU, CPF {cpf}" })).Content.ReadFromJsonAsync<JsonElement>(Json.Opcoes);
        var id = criada.GetProperty("tarefa").GetProperty("id").GetInt32();
        await http.PostAsJsonAsync($"/api/tarefas/{id}/proximo-passo",
            new { url = "https://meuiptu.sf.prefeitura.sp.gov.br/", conteudo = $"Contribuinte: {cpf}" });

        var tarefa = await http.GetStringAsync($"/api/tarefas/{id}");
        var auditoria = await http.GetStringAsync("/api/auditoria");

        Assert.DoesNotContain(cpf, string.Concat(enviados));       // sink: LLM
        Assert.Contains("[CPF]", string.Concat(enviados));
        Assert.DoesNotContain(cpf, string.Join('\n', logs.Linhas)); // sink: logs
        Assert.DoesNotContain(cpf, tarefa);                         // sink: banco (tarefa e passos)
        Assert.DoesNotContain(cpf, auditoria);                      // sink: EventoAuditoria.Detalhe
        Assert.Contains("Bloqueamos o envio de seu CPF", auditoria); // evento traduzido em linguagem simples
    }

    [Fact]
    public async Task Extensao_registra_bloqueio_sem_texto_livre()
    {
        var http = fabrica.CreateClient();
        var r = await http.PostAsJsonAsync("/api/auditoria/eventos",
            new { sink = "llm", encontrados = new[] { "cpf", "email", "<script>texto livre 123.456.789-09</script>" } });
        var e = await r.Content.ReadFromJsonAsync<JsonElement>(Json.Opcoes);
        Assert.Equal("Sink: llm. Encontrado: cpf, email", e.GetProperty("detalhe").GetString());
        Assert.Equal("baixo", e.GetProperty("nivelRisco").GetString());
        Assert.Equal("Escondemos seu CPF e seu e-mail antes de qualquer coisa sair do seu computador.", e.GetProperty("traducao").GetString());
    }

    [Fact]
    public async Task Senha_no_pedido_bloqueada_na_extensao_vira_SenhaNoChat_alto()
    {
        // A extensão nunca envia o pedido com senha (CT-25); só avisa o tipo encontrado.
        var http = fabrica.CreateClient();
        var r = await http.PostAsJsonAsync("/api/auditoria/eventos", new { sink = "api", encontrados = new[] { "senha" } });
        var e = await r.Content.ReadFromJsonAsync<JsonElement>(Json.Opcoes);
        Assert.Equal("SenhaNoChat", e.GetProperty("tipo").GetString());
        Assert.Equal("alto", e.GetProperty("nivelRisco").GetString());
    }
}
