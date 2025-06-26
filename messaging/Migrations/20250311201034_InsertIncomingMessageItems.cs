using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    public partial class InsertIncomingMessageItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createProcedure = "CREATE PROCEDURE [dbo].[CreateIncomingMessageItem] @Message nvarchar(max), @MessageId nvarchar(max), @Source char(3), @JurisdictionId char(2), @MessageType nvarchar(max), @CertificateNumber char(6), @CreatedDate datetime, @UpdatedDate datetime, @ProcessedStatus char(10), @EventType char(3), @IGVersion char(20) NULL, @EventYear bigint AS BEGIN INSERT INTO IncomingMessageItems (Message, MessageId, Source, JurisdictionId, MessageType, CertificateNumber, CreatedDate, UpdatedDate, ProcessedStatus, EventType, EventYear, IGVersion) VALUES (@Message, @MessageId, @Source, @JurisdictionId, @MessageType, @CertificateNumber, GETDATE(), GETDATE(), @ProcessedStatus, @EventType, @EventYear, @IGVersion) DECLARE @ObjectID int; SET @ObjectID = SCOPE_IDENTITY(); SELECT * FROM IncomingMessageItems Where Id = @ObjectID; RETURN END";
            migrationBuilder.Sql(createProcedure);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var dropProcedure = "DROP PROCEDURE [dbo].[CreateIncomingMessageItem]";
            migrationBuilder.Sql(dropProcedure);
        }
    }
}
