using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfMasterIndex.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addtagstoscanpaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanPathTag",
                columns: table => new
                {
                    ScanPathsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanPathTag", x => new { x.ScanPathsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ScanPathTag_ScanPaths_ScanPathsId",
                        column: x => x.ScanPathsId,
                        principalTable: "ScanPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScanPathTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanPathTag_TagsId",
                table: "ScanPathTag",
                column: "TagsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanPathTag");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
