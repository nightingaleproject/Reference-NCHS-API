using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class IGVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IGVersion",
                table: "OutgoingMessageItems",
                type: "CHAR(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IGVersion",
                table: "IncomingMessageItems",
                type: "CHAR(20)",
                maxLength: 20,
                nullable: true);

            // Update IGVersion to "v2.2" for existing backwards-compatible entries where EventType equals "MOR"
            migrationBuilder.Sql("UPDATE OutgoingMessageItems SET IGVersion = 'VRDR_STU2_2' WHERE EventType = 'MOR'");
            migrationBuilder.Sql("UPDATE IncomingMessageItems SET IGVersion = 'VRDR_STU2_2' WHERE EventType = 'MOR'");
            // Update IGVersion to "v2.0" for existing backwards-compatible entries where EventType equals "NAT" or "FET"
            migrationBuilder.Sql("UPDATE OutgoingMessageItems SET IGVersion = 'BFDR_STU2_0' WHERE EventType = 'NAT' or EventType = 'FET'");
            migrationBuilder.Sql("UPDATE IncomingMessageItems SET IGVersion = 'BFDR_STU2_0' WHERE EventType = 'NAT' or EventType = 'FET'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IGVersion",
                table: "OutgoingMessageItems");

            migrationBuilder.DropColumn(
                name: "IGVersion",
                table: "IncomingMessageItems");
        }
    }
}
