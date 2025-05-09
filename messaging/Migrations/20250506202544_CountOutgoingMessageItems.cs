using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class CountOutgoingMessageItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[CountNewOutgoingMessages] @JurisdictionId char(2), @EventType char(3) = NULL AS BEGIN SET NOCOUNT ON; SELECT COUNT(*) FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND RetrievedAt IS NULL END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[CountNewOutgoingMessages]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
