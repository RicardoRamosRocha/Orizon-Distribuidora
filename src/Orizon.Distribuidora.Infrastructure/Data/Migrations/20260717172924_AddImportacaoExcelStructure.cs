using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Distribuidora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportacaoExcelStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelosImportacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TipoArquivo = table.Column<int>(type: "integer", nullable: false),
                    MapeamentoColunasJson = table.Column<string>(type: "jsonb", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosImportacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosImportacao_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportacoesHistorico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeloImportacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    NomeArquivo = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    TipoArquivo = table.Column<int>(type: "integer", nullable: false),
                    TamanhoArquivoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalLinhas = table.Column<int>(type: "integer", nullable: false),
                    LinhasValidas = table.Column<int>(type: "integer", nullable: false),
                    LinhasComErro = table.Column<int>(type: "integer", nullable: false),
                    LinhasImportadas = table.Column<int>(type: "integer", nullable: false),
                    IniciadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportacoesHistorico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportacoesHistorico_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportacoesHistorico_ModelosImportacao_ModeloImportacaoId",
                        column: x => x.ModeloImportacaoId,
                        principalTable: "ModelosImportacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportacaoItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportacaoHistoricoId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroLinha = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DadosOriginaisJson = table.Column<string>(type: "jsonb", nullable: false),
                    DadosNormalizadosJson = table.Column<string>(type: "jsonb", nullable: true),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportacaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportacaoItens_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportacaoItens_ImportacoesHistorico_ImportacaoHistoricoId",
                        column: x => x.ImportacaoHistoricoId,
                        principalTable: "ImportacoesHistorico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportacaoItens_Products_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportacaoErros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportacaoHistoricoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportacaoItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumeroLinha = table.Column<int>(type: "integer", nullable: true),
                    Coluna = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ValorOriginal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportacaoErros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportacaoErros_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportacaoErros_ImportacaoItens_ImportacaoItemId",
                        column: x => x.ImportacaoItemId,
                        principalTable: "ImportacaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportacaoErros_ImportacoesHistorico_ImportacaoHistoricoId",
                        column: x => x.ImportacaoHistoricoId,
                        principalTable: "ImportacoesHistorico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoErros_CompanyId_ImportacaoHistoricoId",
                table: "ImportacaoErros",
                columns: new[] { "CompanyId", "ImportacaoHistoricoId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoErros_CompanyId_NumeroLinha",
                table: "ImportacaoErros",
                columns: new[] { "CompanyId", "NumeroLinha" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoErros_ImportacaoHistoricoId",
                table: "ImportacaoErros",
                column: "ImportacaoHistoricoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoErros_ImportacaoItemId",
                table: "ImportacaoErros",
                column: "ImportacaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoItens_CompanyId_ImportacaoHistoricoId_NumeroLinha",
                table: "ImportacaoItens",
                columns: new[] { "CompanyId", "ImportacaoHistoricoId", "NumeroLinha" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoItens_CompanyId_Status",
                table: "ImportacaoItens",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoItens_ImportacaoHistoricoId",
                table: "ImportacaoItens",
                column: "ImportacaoHistoricoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoItens_ProdutoId",
                table: "ImportacaoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesHistorico_CompanyId_CreatedAt",
                table: "ImportacoesHistorico",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesHistorico_CompanyId_NomeArquivo",
                table: "ImportacoesHistorico",
                columns: new[] { "CompanyId", "NomeArquivo" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesHistorico_CompanyId_Status",
                table: "ImportacoesHistorico",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesHistorico_ModeloImportacaoId",
                table: "ImportacoesHistorico",
                column: "ModeloImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosImportacao_CompanyId_Ativo",
                table: "ModelosImportacao",
                columns: new[] { "CompanyId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosImportacao_CompanyId_Nome",
                table: "ModelosImportacao",
                columns: new[] { "CompanyId", "Nome" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportacaoErros");

            migrationBuilder.DropTable(
                name: "ImportacaoItens");

            migrationBuilder.DropTable(
                name: "ImportacoesHistorico");

            migrationBuilder.DropTable(
                name: "ModelosImportacao");
        }
    }
}
