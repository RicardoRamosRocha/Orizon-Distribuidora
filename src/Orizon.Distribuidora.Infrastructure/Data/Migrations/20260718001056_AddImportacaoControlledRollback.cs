using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Distribuidora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportacaoControlledRollback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FalhasRollback",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ObservacoesRollback",
                table: "ImportacoesHistorico",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosBloqueadosRollback",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosRemovidosRollback",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosRestauradosRollback",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RollbackFinalizadoEm",
                table: "ImportacoesHistorico",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RollbackIniciadoEm",
                table: "ImportacoesHistorico",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioRollbackId",
                table: "ImportacoesHistorico",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensagemRollback",
                table: "ImportacaoItens",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Revertido",
                table: "ImportacaoItens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RollbackExecutadoEm",
                table: "ImportacaoItens",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FalhasRollback",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ObservacoesRollback",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosBloqueadosRollback",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosRemovidosRollback",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosRestauradosRollback",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "RollbackFinalizadoEm",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "RollbackIniciadoEm",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "UsuarioRollbackId",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "MensagemRollback",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "Revertido",
                table: "ImportacaoItens");

            migrationBuilder.DropColumn(
                name: "RollbackExecutadoEm",
                table: "ImportacaoItens");
        }
    }
}
