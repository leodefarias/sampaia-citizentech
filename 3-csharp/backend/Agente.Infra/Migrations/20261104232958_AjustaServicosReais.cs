using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agente.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AjustaServicosReais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DominiosSensiveis", "Modo" },
                values: new object[] { "novaagenciavirtual.sabesp.com.br/auth/login", "Login" });

            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 2,
                column: "UrlBase",
                value: "https://meuiptu.sf.prefeitura.sp.gov.br/servicos/pagamentos");

            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DominiosSensiveis", "Modo", "UrlBase" },
                values: new object[] { "sso.acesso.gov.br", "Login", "https://www3.fazenda.sp.gov.br/SIVEI/DebitosVinculados/Consulta" });

            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 5,
                column: "DominiosSensiveis",
                value: "127.0.0.1:5174");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DominiosSensiveis", "Modo" },
                values: new object[] { null, "Publico" });

            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 2,
                column: "UrlBase",
                value: "https://meuiptu.sf.prefeitura.sp.gov.br/");

            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DominiosSensiveis", "Modo", "UrlBase" },
                values: new object[] { null, "Publico", "https://www.ipva.fazenda.sp.gov.br/" });

            migrationBuilder.UpdateData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 5,
                column: "DominiosSensiveis",
                value: "sso.acesso.gov.br");
        }
    }
}
