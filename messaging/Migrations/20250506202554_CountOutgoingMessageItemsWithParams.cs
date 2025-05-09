using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class CountOutgoingMessageItemsWithParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[CountNewOutgoingMessagesWithParams] @JurisdictionId char(2), @EventYear bigint = NULL, @CertificateNumber char(6) = NULL, @EventType char(3) = NULL, @Since datetime2 = NULL AS BEGIN SET NOCOUNT ON;  SELECT COUNT(*) FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventYear = (CASE WHEN @EventYear IS NOT NULL THEN @EventYear ELSE EventYear END) AND CertificateNumber = (CASE WHEN @CertificateNumber IS NOT NULL THEN @CertificateNumber ELSE CertificateNumber END) AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND CreatedDate >= CAST(@Since AS datetime2) END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[CountNewOutgoingMessagesWithParams]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
