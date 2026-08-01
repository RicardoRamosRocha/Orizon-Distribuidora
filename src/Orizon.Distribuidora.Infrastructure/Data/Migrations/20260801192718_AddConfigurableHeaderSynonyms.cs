using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Distribuidora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableHeaderSynonyms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeaderSynonyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampoDestino = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sinonimo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Prioridade = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Origem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeaderSynonyms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeaderSynonyms_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeaderSynonyms_CompanyId_Ativo_CampoDestino_Prioridade",
                table: "HeaderSynonyms",
                columns: new[] { "CompanyId", "Ativo", "CampoDestino", "Prioridade" });

            migrationBuilder.CreateIndex(
                name: "IX_HeaderSynonyms_CompanyId_CampoDestino_Sinonimo",
                table: "HeaderSynonyms",
                columns: new[] { "CompanyId", "CampoDestino", "Sinonimo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeaderSynonyms");
        }
    }
}
