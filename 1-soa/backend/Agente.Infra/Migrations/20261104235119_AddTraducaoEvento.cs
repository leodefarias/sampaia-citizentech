using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agente.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTraducaoEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Traducao",
                table: "SAMPAIA_EVENTO",
                type: "NVARCHAR2(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Traducao",
                table: "SAMPAIA_EVENTO");
        }
    }
}
