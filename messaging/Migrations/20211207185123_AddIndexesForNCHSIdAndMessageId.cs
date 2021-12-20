using Microsoft.EntityFrameworkCore.Migrations;

namespace messaging.Migrations
{
    public partial class AddIndexesForNCHSIdAndMessageId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NCHSIdentifier",
                table: "IncomingMessageLogs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MessageId",
                table: "IncomingMessageLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessageLogs_MessageId",
                table: "IncomingMessageLogs",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingMessageLogs_NCHSIdentifier",
                table: "IncomingMessageLogs",
                column: "NCHSIdentifier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingMessageLogs_MessageId",
                table: "IncomingMessageLogs");

            migrationBuilder.DropIndex(
                name: "IX_IncomingMessageLogs_NCHSIdentifier",
                table: "IncomingMessageLogs");

            migrationBuilder.AlterColumn<string>(
                name: "NCHSIdentifier",
                table: "IncomingMessageLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MessageId",
                table: "IncomingMessageLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
