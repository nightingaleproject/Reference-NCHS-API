using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class AddMetaDataColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CertificateNumber",
                table: "OutgoingMessageItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "OutgoingMessageItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EventYear",
                table: "OutgoingMessageItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CertificateNumber",
                table: "IncomingMessageItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "IncomingMessageItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EventYear",
                table: "IncomingMessageItems",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateNumber",
                table: "OutgoingMessageItems");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "OutgoingMessageItems");

            migrationBuilder.DropColumn(
                name: "EventYear",
                table: "OutgoingMessageItems");

            migrationBuilder.DropColumn(
                name: "CertificateNumber",
                table: "IncomingMessageItems");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "IncomingMessageItems");

            migrationBuilder.DropColumn(
                name: "EventYear",
                table: "IncomingMessageItems");
        }
    }
}
