﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NVSSMessaging.Migrations
{
    public partial class AddCreatedAtToFHIRMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTimestamp",
                table: "FHIRMessageItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedTimestamp",
                table: "FHIRMessageItems");
        }
    }
}
