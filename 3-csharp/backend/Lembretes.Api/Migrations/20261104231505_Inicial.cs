using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lembretes.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SAMPAIA_LEMBRETE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    DocumentoId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Titulo = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    DataVencimento = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    DataAviso = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    Canal = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    Concluido = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_LEMBRETE", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SAMPAIA_LEMBRETE",
                columns: new[] { "Id", "Canal", "Concluido", "DataAviso", "DataVencimento", "DocumentoId", "Titulo" },
                values: new object[,]
                {
                    { 1, "Esp32", false, "2026-11-19", "2026-11-20", null, "Conta de água" },
                    { 2, "App", false, "2026-11-24", "2026-11-25", null, "IPTU parcela 11" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_LEMBRETE_Concluido_DataVencimento",
                table: "SAMPAIA_LEMBRETE",
                columns: new[] { "Concluido", "DataVencimento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SAMPAIA_LEMBRETE");
        }
    }
}
