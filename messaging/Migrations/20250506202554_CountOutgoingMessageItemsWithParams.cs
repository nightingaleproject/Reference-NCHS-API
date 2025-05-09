using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class CountOutgoingMessageItemsWithParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[CountNewOutgoingMessagesWithParams] @JurisdictionId char(2), @EventYear bigint = NULL, @CertificateNumber char(6) = NULL, @EventType char(3) = NULL, @Since datetime = NULL AS BEGIN SET NOCOUNT ON;  SELECT CAST(COUNT(*) AS bigint) AS Id, 'NA' AS CertificateNumber, GETDATE() AS CreatedDate, GETDATE() AS UpdatedDate, 'NA' AS EventType, 'NA' AS Message, 'NA' AS MessageType, 'NA' AS MessageId, 'NA' AS JurisdictionId, CAST(1 AS bigint) AS EventYear, NULL AS RetrievedAt FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventYear = (CASE WHEN @EventYear IS NOT NULL THEN @EventYear ELSE EventYear END) AND CertificateNumber = (CASE WHEN @CertificateNumber IS NOT NULL THEN @CertificateNumber ELSE CertificateNumber END) AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND CreatedDate >= (CASE WHEN @Since IS NOT NULL THEN @Since ELSE CreatedDate END) END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[CountNewOutgoingMessagesWithParams]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
