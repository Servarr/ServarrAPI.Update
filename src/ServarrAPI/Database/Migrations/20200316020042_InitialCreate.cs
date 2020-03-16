using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ServarrAPI.Database.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Updates",
                columns: table => new
                {
                    UpdateEntityId = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Version = table.Column<string>(nullable: false),
                    ReleaseDate = table.Column<DateTime>(nullable: false),
                    Branch = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    New = table.Column<string>(nullable: true),
                    Fixed = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Updates", x => x.UpdateEntityId);
                });

            migrationBuilder.CreateTable(
                name: "UpdateFiles",
                columns: table => new
                {
                    UpdateEntityId = table.Column<int>(nullable: false),
                    OperatingSystem = table.Column<int>(nullable: false),
                    Runtime = table.Column<int>(nullable: false),
                    Architecture = table.Column<int>(nullable: false),
                    Filename = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateFiles", x => new { x.UpdateEntityId, x.OperatingSystem, x.Architecture, x.Runtime });
                    table.ForeignKey(
                        name: "FK_UpdateFiles_Updates_UpdateEntityId",
                        column: x => x.UpdateEntityId,
                        principalTable: "Updates",
                        principalColumn: "UpdateEntityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Updates_Branch_Version",
                table: "Updates",
                columns: new[] { "Branch", "Version" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateFiles");

            migrationBuilder.DropTable(
                name: "Updates");
        }
    }
}
