using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class updatecolumntypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "OutgoingMessageItems",
                type: "CHAR(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CertificateNumber",
                table: "OutgoingMessageItems",
                type: "CHAR(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "IncomingMessageItems",
                type: "CHAR(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CertificateNumber",
                table: "IncomingMessageItems",
                type: "CHAR(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "OutgoingMessageItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "CHAR(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CertificateNumber",
                table: "OutgoingMessageItems",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "CHAR(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "IncomingMessageItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "CHAR(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CertificateNumber",
                table: "IncomingMessageItems",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "CHAR(6)",
                oldMaxLength: 6,
                oldNullable: true);
        }
    }
}
