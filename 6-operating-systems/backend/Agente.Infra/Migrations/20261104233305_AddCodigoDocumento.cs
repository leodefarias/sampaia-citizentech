using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agente.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "SAMPAIA_DOCUMENTO",
                type: "NVARCHAR2(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "SAMPAIA_DOCUMENTO");
        }
    }
}
