using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agente.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPoupatempoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SAMPAIA_SERVICO",
                columns: new[] { "Id", "Chave", "DominiosSensiveis", "Modo", "Nome", "Orgao", "UrlBase" },
                values: new object[] { 6, "poupatempo-sp", "sso.acesso.gov.br", "Login", "Agendamento no Poupatempo", "Poupatempo SP", "https://www.poupatempo.sp.gov.br/" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SAMPAIA_SERVICO",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
