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
                type: "CHAR(10)",
            maxLength: 10,
                nullable: false,
                defaultValue: "v2.2");

            migrationBuilder.AddColumn<string>(
                name: "IGVersion",
                table: "IncomingMessageItems",
                type: "CHAR(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "v2.2");
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
