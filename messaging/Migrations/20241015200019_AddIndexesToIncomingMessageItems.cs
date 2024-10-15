using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class AddIndexesToIncomingMessageItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessageItems_CreatedDate",
                table: "IncomingMessageItems",
                column: "CreatedDate");
            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessageItems_UpdatedDate",
                table: "IncomingMessageItems",
                column: "UpdatedDate");
            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessageItems_ProcessedStatus",
                table: "IncomingMessageItems",
                column: "ProcessedStatus");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingMessageItems_CreatedDate",
                table: "IncomingMessageItems");
            migrationBuilder.DropIndex(
                name: "IX_IncomingMessageItems_UpdatedDate",
                table: "IncomingMessageItems");
            migrationBuilder.DropIndex(
                name: "IX_IncomingMessageItems_ProcessedStatus",
                table: "IncomingMessageItems");
        }
    }
}
