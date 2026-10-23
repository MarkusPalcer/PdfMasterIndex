using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PdfMasterIndex.Service.Migrations
{
    /// <inheritdoc />
    public partial class addindices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Occurrences_WordId",
                table: "Occurrences");

            migrationBuilder.CreateIndex(
                name: "IX_Words_Value",
                table: "Words",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_WordId_DocumentId_Page",
                table: "Occurrences",
                columns: new[] { "WordId", "DocumentId", "Page" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Words_Value",
                table: "Words");

            migrationBuilder.DropIndex(
                name: "IX_Occurrences_WordId_DocumentId_Page",
                table: "Occurrences");

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_WordId",
                table: "Occurrences",
                column: "WordId");
        }
    }
}
