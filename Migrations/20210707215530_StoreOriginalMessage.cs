using Microsoft.EntityFrameworkCore.Migrations;

namespace NVSSMessaging.Migrations
{
    public partial class StoreOriginalMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Filename",
                table: "FHIRMessageItems",
                newName: "SubmissionMessage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubmissionMessage",
                table: "FHIRMessageItems",
                newName: "Filename");
        }
    }
}
