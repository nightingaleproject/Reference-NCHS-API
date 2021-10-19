using Microsoft.EntityFrameworkCore.Migrations;

namespace messaging.Migrations
{
    public partial class SwitchCertNumberToNCHSIdentifier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateNumber",
                table: "IncomingMessageLogs");

            migrationBuilder.AddColumn<string>(
                name: "NCHSIdentifier",
                table: "IncomingMessageLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NCHSIdentifier",
                table: "IncomingMessageLogs");

            migrationBuilder.AddColumn<long>(
                name: "CertificateNumber",
                table: "IncomingMessageLogs",
                type: "bigint",
                nullable: true);
        }
    }
}
