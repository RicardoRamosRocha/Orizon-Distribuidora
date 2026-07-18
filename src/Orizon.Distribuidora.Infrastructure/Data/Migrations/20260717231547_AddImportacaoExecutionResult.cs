using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Distribuidora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportacaoExecutionResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FalhasExecucao",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItensBloqueados",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosAtualizados",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosInseridos",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SemAlteracao",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TokenExecucao",
                table: "ImportacoesHistorico",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioExecutorId",
                table: "ImportacoesHistorico",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlteracoesAplicadasJson",
                table: "ImportacaoItens",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChaveIdempotencia",
                table: "ImportacaoItens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExecutadoEm",
                table: "ImportacaoItens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensagemExecucao",
                table: "ImportacaoItens",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperacaoExecucao",
                table: "ImportacaoItens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ImportacoesHistorico_TokenExecucao",
                table: "ImportacoesHistorico",
                column: "TokenExecucao",
                unique: true,
                filter: "\"TokenExecucao\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoItens_CompanyId_ChaveIdempotencia",
                table: "ImportacaoItens",
                columns: new[] { "CompanyId", "ChaveIdempotencia" },
                unique: true,
                filter: "\"ChaveIdempotencia\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportacaoItens_CompanyId_ImportacaoHistoricoId_Status",
                table: "ImportacaoItens",
                columns: new[] { "CompanyId", "ImportacaoHistoricoId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportacoesHistorico_TokenExecucao",
                table: "ImportacoesHistorico");

            migrationBuilder.DropIndex(
                name: "IX_ImportacaoItens_CompanyId_ChaveIdempotencia",
                table: "ImportacaoItens");

            migrationBuilder.DropIndex(
                name: "IX_ImportacaoItens_CompanyId_ImportacaoHistoricoId_Status",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "FalhasExecucao",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ItensBloqueados",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosAtualizados",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosInseridos",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "SemAlteracao",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "TokenExecucao",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "UsuarioExecutorId",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "AlteracoesAplicadasJson",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "ChaveIdempotencia",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "ExecutadoEm",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "MensagemExecucao",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "OperacaoExecucao",
                table: "ImportacaoItens");
        }
    }
}
