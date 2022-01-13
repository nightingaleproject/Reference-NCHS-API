using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class AddTypeToIncomingMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "IncomingMessageItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "IncomingMessageItems");
        }
    }
}
