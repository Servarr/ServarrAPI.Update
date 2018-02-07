using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace RadarrAPI.Database.Migrations
{
    public partial class AddIntegrityCheckUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "Updates",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Updates_Branch_Version",
                table: "Updates",
                columns: new[] { "Branch", "Version" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Updates_Branch_Version",
                table: "Updates");

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "Updates",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
