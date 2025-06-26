using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class SelectOutgoingMessageItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[SelectNewOutgoingMessageOrdered] @JurisdictionId char(2), @EventType char(3) = NULL, @IGVersion char(20) = NULL, @Count int AS BEGIN SET NOCOUNT ON; SELECT * FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND IGVersion = @IGVersion AND RetrievedAt IS NULL ORDER BY CreatedDate OFFSET 0 ROWS FETCH NEXT @Count ROWS ONLY END;";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[SelectNewOutgoingMessageOrdered]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
