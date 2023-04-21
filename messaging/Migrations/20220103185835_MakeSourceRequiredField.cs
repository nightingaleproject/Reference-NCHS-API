using Microsoft.EntityFrameworkCore.Migrations;

namespace messaging.Migrations
{
    public partial class MakeSourceRequiredField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "IncomingMessageItems",
                type: "CHAR(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "CHAR(3)",
                oldMaxLength: 3,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "IncomingMessageItems",
                type: "CHAR(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "CHAR(3)",
                oldMaxLength: 3);
        }
    }
}
