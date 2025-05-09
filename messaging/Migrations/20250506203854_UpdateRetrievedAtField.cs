using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class UpdateRetrievedAtField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[UpdateOutgoingMessagesRetrievedAt] @Id int, @RetrievedAt datetime AS BEGIN SET NOCOUNT ON UPDATE OutgoingMessageItems SET RetrievedAt = @RetrievedAt WHERE Id = @Id END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[UpdateOutgoingMessagesRetrievedAt]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
