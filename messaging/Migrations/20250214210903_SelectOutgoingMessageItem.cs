using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class SelectOutgoingMessageItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[SelectNewOutgoingMessageItemsWithParams] @JurisdictionId char(2), @EventYear bigint = NULL, @CertificateNumber char(6) = NULL, @EventType char(3) = NULL AS BEGIN SET NOCOUNT ON; SELECT * FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventYear = (CASE WHEN @EventYear IS NOT NULL THEN @EventYear ELSE EventYear END) AND CertificateNumber = (CASE WHEN @CertificateNumber IS NOT NULL THEN @CertificateNumber ELSE CertificateNumber END) AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[SelectNewOutgoingMessageItemsWithParams]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
