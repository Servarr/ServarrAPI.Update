using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RadarrAPI.Database.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Updates",
                columns: table => new
                {
                    UpdateEntityId = table.Column<int>(nullable: false).Annotation("MySql:ValueGeneratedOnAdd", true),
                    Version = table.Column<string>(nullable: true),
                    ReleaseDate = table.Column<DateTime>(nullable: false),
                    New = table.Column<string>(nullable: true),
                    Fixed = table.Column<string>(nullable: true),
                    Branch = table.Column<int>(nullable: false)
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
                    Filename = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateFiles", x => new { x.UpdateEntityId, x.OperatingSystem });
                    table.ForeignKey(
                        name: "FK_UpdateFiles_Updates_UpdateEntityId",
                        column: x => x.UpdateEntityId,
                        principalTable: "Updates",
                        principalColumn: "UpdateEntityId",
                        onDelete: ReferentialAction.Cascade);
                });
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
