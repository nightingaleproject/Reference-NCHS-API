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
                type: "CHAR(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IGVersion",
                table: "IncomingMessageItems",
                type: "CHAR(5)",
                maxLength: 5,
                nullable: true);
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
