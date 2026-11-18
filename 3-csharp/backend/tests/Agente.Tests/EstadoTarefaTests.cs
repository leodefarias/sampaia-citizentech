using Agente.Domain;

namespace Agente.Tests;

public class EstadoTarefaTests
{
    private static Tarefa Nova() => new() { Intencao = "pagar IPTU" };

    [Fact]
    public void Caminho_completo_com_login_e_confirmacao()
    {
        var t = Nova();
        t.MudarPara(EstadoTarefa.Navegando);
        t.MudarPara(EstadoTarefa.AguardandoLogin);
        t.MudarPara(EstadoTarefa.Navegando);
        t.MudarPara(EstadoTarefa.AguardandoConfirmacao);
        t.MudarPara(EstadoTarefa.Concluida);
        Assert.True(t.Finalizada);
    }

    [Theory]
    [InlineData(EstadoTarefa.AguardandoLogin)]
    [InlineData(EstadoTarefa.AguardandoConfirmacao)]
    [InlineData(EstadoTarefa.Concluida)]
    public void Planejando_so_vai_para_navegando(EstadoTarefa destino) =>
        Assert.Throws<TransicaoInvalidaException>(() => Nova().MudarPara(destino));

    [Fact]
    public void Nao_pula_confirmacao_vindo_do_login()
    {
        var t = Nova();
        t.MudarPara(EstadoTarefa.Navegando);
        t.MudarPara(EstadoTarefa.AguardandoLogin);
        Assert.Throws<TransicaoInvalidaException>(() => t.MudarPara(EstadoTarefa.Concluida));
    }

    [Theory]
    [InlineData(EstadoTarefa.Planejando)]
    [InlineData(EstadoTarefa.Navegando)]
    [InlineData(EstadoTarefa.AguardandoLogin)]
    [InlineData(EstadoTarefa.AguardandoConfirmacao)]
    public void Qualquer_estado_aberto_pode_falhar(EstadoTarefa de)
    {
        foreach (var para in Enum.GetValues<EstadoTarefa>())
            if (para == EstadoTarefa.Falhou) Assert.True(Tarefa.PodeIr(de, para));
    }

    [Theory]
    [InlineData(EstadoTarefa.Concluida)]
    [InlineData(EstadoTarefa.Falhou)]
    public void Estado_final_nao_sai_do_lugar(EstadoTarefa final) =>
        Assert.All(Enum.GetValues<EstadoTarefa>(), para => Assert.False(Tarefa.PodeIr(final, para)));
}
