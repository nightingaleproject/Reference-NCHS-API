using Microsoft.EntityFrameworkCore.Migrations;

namespace messaging.Migrations
{
    public partial class AddIndexToOutgoingMessageItemsCreatedDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutgoingMessageItems_CreatedDate",
                table: "OutgoingMessageItems",
                column: "CreatedDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutgoingMessageItems_CreatedDate",
                table: "OutgoingMessageItems");
        }
    }
}
