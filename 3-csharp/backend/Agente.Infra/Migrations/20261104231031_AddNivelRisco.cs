using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agente.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddNivelRisco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NivelRisco",
                table: "SAMPAIA_EVENTO",
                type: "NVARCHAR2(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Baixo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NivelRisco",
                table: "SAMPAIA_EVENTO");
        }
    }
}
