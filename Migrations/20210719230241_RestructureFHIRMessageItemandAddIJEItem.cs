using Microsoft.EntityFrameworkCore.Migrations;

namespace NVSSMessaging.Migrations
{
    public partial class RestructureFHIRMessageItemandAddIJEItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ACKMessage",
                table: "FHIRMessageItems");

            migrationBuilder.DropColumn(
                name: "IJE",
                table: "FHIRMessageItems");

            migrationBuilder.RenameColumn(
                name: "SubmissionMessage",
                table: "FHIRMessageItems",
                newName: "Message");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Message",
                table: "FHIRMessageItems",
                newName: "SubmissionMessage");

            migrationBuilder.AddColumn<string>(
                name: "ACKMessage",
                table: "FHIRMessageItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IJE",
                table: "FHIRMessageItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
