using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orizon.Distribuidora.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceImportacaoMappingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssinaturaColunas",
                table: "ModelosImportacao",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Padrao",
                table: "ModelosImportacao",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "ModelosImportacao",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssinaturaColunas",
                table: "ModelosImportacao");

            migrationBuilder.DropColumn(
                name: "Padrao",
                table: "ModelosImportacao");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "ModelosImportacao");
        }
    }
}
