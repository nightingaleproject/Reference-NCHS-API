using Microsoft.EntityFrameworkCore.Migrations;

namespace messaging.Migrations
{
    public partial class AddJurisdictionIdToAllTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JurisdictionId",
                table: "OutgoingMessageItems",
                type: "CHAR(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JurisdictionId",
                table: "IncomingMessageLogs",
                type: "CHAR(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JurisdictionId",
                table: "IncomingMessageItems",
                type: "CHAR(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JurisdictionId",
                table: "OutgoingMessageItems");

            migrationBuilder.DropColumn(
                name: "JurisdictionId",
                table: "IncomingMessageLogs");

            migrationBuilder.DropColumn(
                name: "JurisdictionId",
                table: "IncomingMessageItems");
        }
    }
}
