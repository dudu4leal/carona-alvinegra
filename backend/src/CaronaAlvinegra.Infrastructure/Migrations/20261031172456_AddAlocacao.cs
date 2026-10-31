using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaronaAlvinegra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlocacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alocacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JogoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rota = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GruposEscalados = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alocacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alocacoes_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpinsAlocados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ocupacao = table.Column<int>(type: "integer", nullable: false),
                    AlocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrupoIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpinsAlocados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpinsAlocados_Alocacoes_AlocacaoId",
                        column: x => x.AlocacaoId,
                        principalTable: "Alocacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VansAlocadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Motorista = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ocupacao = table.Column<int>(type: "integer", nullable: false),
                    AlocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrupoIds = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VansAlocadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VansAlocadas_Alocacoes_AlocacaoId",
                        column: x => x.AlocacaoId,
                        principalTable: "Alocacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alocacoes_JogoId",
                table: "Alocacoes",
                column: "JogoId");

            migrationBuilder.CreateIndex(
                name: "IX_SpinsAlocados_AlocacaoId",
                table: "SpinsAlocados",
                column: "AlocacaoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VansAlocadas_AlocacaoId",
                table: "VansAlocadas",
                column: "AlocacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpinsAlocados");

            migrationBuilder.DropTable(
                name: "VansAlocadas");

            migrationBuilder.DropTable(
                name: "Alocacoes");
        }
    }
}
