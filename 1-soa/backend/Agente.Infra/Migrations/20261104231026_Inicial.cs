using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Agente.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SAMPAIA_SERVICO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Chave = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Orgao = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Modo = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    UrlBase = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    DominiosSensiveis = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_SERVICO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SAMPAIA_USUARIO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Nome = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    PreferenciasAcessibilidade = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_USUARIO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SAMPAIA_TAREFA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UsuarioId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ServicoId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Intencao = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_TAREFA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SAMPAIA_TAREFA_SAMPAIA_SERVICO_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "SAMPAIA_SERVICO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SAMPAIA_TAREFA_SAMPAIA_USUARIO_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "SAMPAIA_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SAMPAIA_DOCUMENTO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TarefaId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Tipo = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Valor = table.Column<decimal>(type: "DECIMAL(12,2)", precision: 12, scale: 2, nullable: true),
                    Vencimento = table.Column<string>(type: "NVARCHAR2(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_DOCUMENTO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SAMPAIA_DOCUMENTO_SAMPAIA_TAREFA_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "SAMPAIA_TAREFA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SAMPAIA_EVENTO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TarefaId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Tipo = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Detalhe = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Em = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_EVENTO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SAMPAIA_EVENTO_SAMPAIA_TAREFA_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "SAMPAIA_TAREFA",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SAMPAIA_PASSO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TarefaId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Acao = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Seletor = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ExigeConfirmacao = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Resultado = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Em = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAMPAIA_PASSO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SAMPAIA_PASSO_SAMPAIA_TAREFA_TarefaId",
                        column: x => x.TarefaId,
                        principalTable: "SAMPAIA_TAREFA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SAMPAIA_SERVICO",
                columns: new[] { "Id", "Chave", "DominiosSensiveis", "Modo", "Nome", "Orgao", "UrlBase" },
                values: new object[,]
                {
                    { 1, "sabesp", null, "Publico", "2ª via da conta de água", "Sabesp", "https://agenciavirtual.sabesp.com.br/" },
                    { 2, "iptu", null, "Publico", "Guia do IPTU", "Prefeitura de São Paulo", "https://meuiptu.sf.prefeitura.sp.gov.br/" },
                    { 3, "ipva", null, "Publico", "Consulta e pagamento do IPVA", "Sefaz-SP", "https://www.ipva.fazenda.sp.gov.br/" },
                    { 4, "sp156", null, "Anonimo", "Pedido de zeladoria SP156", "Prefeitura de São Paulo", "https://sp156.prefeitura.sp.gov.br/portal/servicos" },
                    { 5, "poupatempo", "sso.acesso.gov.br", "Login", "Agendamento no Poupatempo (simulado)", "Portal simulado", "http://localhost:5173/" }
                });

            migrationBuilder.InsertData(
                table: "SAMPAIA_USUARIO",
                columns: new[] { "Id", "Nome", "PreferenciasAcessibilidade" },
                values: new object[] { 1, "Dona Cida (simulada)", "fonte-grande" });

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_DOCUMENTO_TarefaId",
                table: "SAMPAIA_DOCUMENTO",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_EVENTO_TarefaId",
                table: "SAMPAIA_EVENTO",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_PASSO_TarefaId",
                table: "SAMPAIA_PASSO",
                column: "TarefaId");

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_SERVICO_Chave",
                table: "SAMPAIA_SERVICO",
                column: "Chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_TAREFA_ServicoId",
                table: "SAMPAIA_TAREFA",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_SAMPAIA_TAREFA_UsuarioId",
                table: "SAMPAIA_TAREFA",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SAMPAIA_DOCUMENTO");

            migrationBuilder.DropTable(
                name: "SAMPAIA_EVENTO");

            migrationBuilder.DropTable(
                name: "SAMPAIA_PASSO");

            migrationBuilder.DropTable(
                name: "SAMPAIA_TAREFA");

            migrationBuilder.DropTable(
                name: "SAMPAIA_SERVICO");

            migrationBuilder.DropTable(
                name: "SAMPAIA_USUARIO");
        }
    }
}
