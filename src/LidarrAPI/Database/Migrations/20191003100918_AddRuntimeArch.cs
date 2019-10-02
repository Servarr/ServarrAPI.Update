using System.Runtime.InteropServices;
using LidarrAPI.Update;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LidarrAPI.Database.Migrations
{
    public partial class AddRuntimeArch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UpdateFiles_Updates_UpdateEntityId",
                table: "UpdateFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UpdateFiles",
                table: "UpdateFiles");

            migrationBuilder.AddColumn<int>(
                name: "Architecture",
                table: "UpdateFiles",
                nullable: false,
                defaultValue: Architecture.X64);

            migrationBuilder.AddColumn<int>(
                name: "Runtime",
                table: "UpdateFiles",
                nullable: false,
                defaultValue: Runtime.DotNet);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UpdateFiles",
                table: "UpdateFiles",
                columns: new[] { "UpdateEntityId", "OperatingSystem", "Architecture", "Runtime" });

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateFiles_Updates_UpdateEntityId",
                table: "UpdateFiles",
                column: "UpdateEntityId",
                principalTable: "Updates",
                principalColumn: "UpdateEntityId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UpdateFiles_Updates_UpdateEntityId",
                table: "UpdateFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UpdateFiles",
                table: "UpdateFiles");

            migrationBuilder.DropColumn(
                name: "Architecture",
                table: "UpdateFiles");

            migrationBuilder.DropColumn(
                name: "Runtime",
                table: "UpdateFiles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UpdateFiles",
                table: "UpdateFiles",
                columns: new[] { "UpdateEntityId", "OperatingSystem" });

            migrationBuilder.AddForeignKey(
                name: "FK_UpdateFiles_Updates_UpdateEntityId",
                table: "UpdateFiles",
                column: "UpdateEntityId",
                principalTable: "Updates",
                principalColumn: "UpdateEntityId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
