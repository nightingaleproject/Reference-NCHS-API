using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class SelectOutgoingMessageItemWithParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[SelectOutgoingMessageItemsWithParamsPaging] @JurisdictionId char(2), @EventYear bigint = NULL, @CertificateNumber char(6) = NULL, @EventType char(3) = NULL, @Since DATETIME = NULL, @Skip int, @Count int AS BEGIN SET NOCOUNT ON; SELECT * FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventYear = (CASE WHEN @EventYear IS NOT NULL THEN @EventYear ELSE EventYear END) AND CertificateNumber = (CASE WHEN @CertificateNumber IS NOT NULL THEN @CertificateNumber ELSE CertificateNumber END) AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND CreatedDate >= (CASE WHEN @Since IS NOT NULL THEN @Since ELSE CreatedDate END) ORDER BY CreatedDate OFFSET @Skip ROWS FETCH NEXT @Count ROWS ONLY END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[SelectOutgoingMessageItemsWithParamsPaging]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
