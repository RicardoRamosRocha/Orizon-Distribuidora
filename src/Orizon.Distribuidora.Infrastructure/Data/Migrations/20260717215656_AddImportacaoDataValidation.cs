using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Distribuidora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportacaoDataValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinhasComAviso",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LinhasDuplicadas",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LinhasIgnoradas",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OpcoesValidacaoJson",
                table: "ImportacoesHistorico",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosAtualizaveis",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosExistentes",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProdutosNovos",
                table: "ImportacoesHistorico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioValidacaoId",
                table: "ImportacoesHistorico",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "ImportacaoErros",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Severidade",
                table: "ImportacaoErros",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinhasComAviso",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "LinhasDuplicadas",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "LinhasIgnoradas",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "OpcoesValidacaoJson",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosAtualizaveis",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosExistentes",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "ProdutosNovos",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "UsuarioValidacaoId",
                table: "ImportacoesHistorico");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "ImportacaoErros");

            migrationBuilder.DropColumn(
                name: "Severidade",
                table: "ImportacaoErros");
        }
    }
}
