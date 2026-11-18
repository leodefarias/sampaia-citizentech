using Agente.Infra;

namespace Agente.Tests;

public class PiiMaskerTests
{
    private static readonly PiiMasker Masker = PiiMasker.Padrao();
    private static string M(string s) => Masker.Mascarar(s).Texto;

    [Fact]
    public void Cpf_valido_com_e_sem_mascara()
    {
        var cpf = Dados.Cpf();
        Assert.Equal("CPF: [CPF]", M($"CPF: {cpf}"));
        Assert.Equal("CPF: [CPF]", M($"CPF: {new string(cpf.Where(char.IsDigit).ToArray())}"));
    }

    [Fact]
    public void Cpf_com_digito_errado_ainda_e_mascarado_como_documento() =>
        Assert.Equal("[DOC]", M("123.456.789-00"));

    [Theory]
    [InlineData("Escreva para maria.teste@exemplo.com.br", "Escreva para [EMAIL]")]
    [InlineData("Ligue (11) 98765-4321", "Ligue [TEL]")]
    [InlineData("RG 12.345.678-9", "RG [DOC]")]
    [InlineData("Renavam 12345678901", "Renavam [DOC]")]
    [InlineData("Cartão 4111 1111 1111 1111", "Cartão [PAGAMENTO]")]
    [InlineData("Linha 00190.00009 01234.567890 12345.678901 1 23450000010000", "Linha [PAGAMENTO]")]
    [InlineData("Pix 00020126330014br.gov.bcb.pix0111teste5204000053039865802BR5909Loja Teste6009SAO PAULO63041A2B fim", "Pix [PAGAMENTO] fim")]
    [InlineData("CEP 01310-100", "CEP [ENDERECO]")]
    [InlineData("Mora na Rua das Flores, 123", "Mora na [ENDERECO]")]
    [InlineData("Fica na rua da Consolação 200", "Fica na [ENDERECO]")]
    [InlineData("Av. Paulista, 1000", "[ENDERECO]")]
    [InlineData("Rua E, 45", "[ENDERECO]")]
    [InlineData("Rua E 45", "[ENDERECO]")]
    // Fala da Paulinha (teste no SP156): "rua e número" é instrução, não endereço.
    [InlineData("Digite o endereço (nome da rua e número), depois escolha na lista.", "Digite o endereço (nome da rua e número), depois escolha na lista.")]
    [InlineData("Diagnóstico pendente", "[SAUDE] pendente")]
    public void Mascara_cada_tipo(string entrada, string esperado) => Assert.Equal(esperado, M(entrada));

    [Fact]
    public void Cartao_sus() => Assert.Equal("CNS [SAUDE]", M($"CNS {Dados.Cns()}"));

    [Theory]
    [InlineData("Senha: abc123")]
    [InlineData("código de verificação = 998877")]
    public void Senha_bloqueia_tudo(string entrada)
    {
        var r = Masker.Mascarar($"Olá. {entrada}");
        Assert.True(r.Bloqueado);
        Assert.Equal(PiiMasker.Bloqueado, r.Texto);
    }

    [Theory]
    [InlineData("Valor R$ 123,45 vence em 10/12/2026")]
    [InlineData("Número SQL 000.000.0000-0")]
    [InlineData("Esqueci minha senha")]
    public void Texto_sem_dado_pessoal_fica_igual(string texto)
    {
        var r = Masker.Mascarar(texto);
        Assert.Equal(texto, r.Texto);
        Assert.False(r.TinhaPii);
    }
}
