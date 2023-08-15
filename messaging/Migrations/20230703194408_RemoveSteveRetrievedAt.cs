using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class RemoveSteveRetrievedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, check each value for the SteveRetrievedAtColumn and, if it has a value, transfer it to that record's RetrievedAt column. If both SteveRetrievedAt and RetrievedAt have data, choose the most recent one to keep in RetrievedAt.
            migrationBuilder.Sql("UPDATE OutgoingMessageItems SET RetrievedAt=SteveRetrievedAt WHERE RetrievedAt IS NULL OR SteveRetrievedAt>RetrievedAt"); // OR RetrivedAt==NULL
            // With all the data preserved in RetreivedAt, remove the SteveRetrieveAt column.
            migrationBuilder.DropColumn(
                name: "SteveRetrievedAt",
                table: "OutgoingMessageItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SteveRetrievedAt",
                table: "OutgoingMessageItems",
                type: "datetime2",
                nullable: true);
        }
    }
}
