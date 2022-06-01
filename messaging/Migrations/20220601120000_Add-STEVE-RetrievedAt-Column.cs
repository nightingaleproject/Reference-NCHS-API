using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    /// <summary>
    /// Enable tracking retrieval of messages by STEVE separately from jurisdictions.
    /// </summary>
    public partial class AddSTEVERetrievedAtColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "STEVE_RetrievedAt",
                table: "OutgoingMessageItems",
                type: "datetime2",
                nullable: true,
                defaultValue: null);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "STEVE_RetrievedAt",
                table: "OutgoingMessageItems");
        }
    }
}
