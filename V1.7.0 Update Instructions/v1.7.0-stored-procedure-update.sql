BEGIN TRANSACTION;
GO

CREATE PROCEDURE [dbo].[SelectNewOutgoingMessageOrdered] @JurisdictionId char(2), @EventType char(3) = NULL, @IGVersion char(20) = NULL, @Count int AS BEGIN SET NOCOUNT ON; SELECT * FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND IGVersion = @IGVersion AND RetrievedAt IS NULL ORDER BY CreatedDate OFFSET 0 ROWS FETCH NEXT @Count ROWS ONLY END;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250626181535_SelectOutgoingMessageItems', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE PROCEDURE [dbo].[CreateIncomingMessageItem] @Message nvarchar(max), @MessageId nvarchar(max), @Source char(3), @JurisdictionId char(2), @MessageType nvarchar(max), @CertificateNumber char(6), @CreatedDate datetime, @UpdatedDate datetime, @ProcessedStatus char(10), @EventType char(3), @IGVersion char(20) NULL, @EventYear bigint AS BEGIN INSERT INTO IncomingMessageItems (Message, MessageId, Source, JurisdictionId, MessageType, CertificateNumber, CreatedDate, UpdatedDate, ProcessedStatus, EventType, EventYear, IGVersion) VALUES (@Message, @MessageId, @Source, @JurisdictionId, @MessageType, @CertificateNumber, GETDATE(), GETDATE(), @ProcessedStatus, @EventType, @EventYear, @IGVersion) DECLARE @ObjectID int; SET @ObjectID = SCOPE_IDENTITY(); SELECT * FROM IncomingMessageItems Where Id = @ObjectID; RETURN END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250626181601_InsertIncomingMessageItem', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE PROCEDURE [dbo].[CountNewOutgoingMessages] @JurisdictionId char(2), @EventType char(3) = NULL, @IGVersion char(20) = NULL AS BEGIN SET NOCOUNT ON; SELECT COUNT(*) FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND IGVersion = @IGVersion AND RetrievedAt IS NULL END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250626185909_CountOutgoingMessageItem', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE PROCEDURE [dbo].[CountNewOutgoingMessagesWithParams] @JurisdictionId char(2), @EventYear bigint = NULL, @IGVersion char(20) = NULL, @CertificateNumber char(6) = NULL, @EventType char(3) = NULL, @Since datetime2 = NULL AS BEGIN SET NOCOUNT ON;  SELECT COUNT(*) FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventYear = (CASE WHEN @EventYear IS NOT NULL THEN @EventYear ELSE EventYear END) AND CertificateNumber = (CASE WHEN @CertificateNumber IS NOT NULL THEN @CertificateNumber ELSE CertificateNumber END) AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND IGVersion = @IGVersion AND CreatedDate >= CAST(@Since AS datetime2) END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250626190047_CountOutgoingMessageItemWithParams', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE PROCEDURE [dbo].[SelectOutgoingMessageItemsWithParamsPaging] @JurisdictionId char(2), @EventYear bigint = NULL, @CertificateNumber char(6) = NULL, @EventType char(3) = NULL, @IGVersion char(20) = NULL, @Since DATETIME2 = NULL, @Skip int, @Count int AS BEGIN SET NOCOUNT ON; SELECT * FROM OutgoingMessageItems WHERE JurisdictionId = @JurisdictionId AND EventYear = (CASE WHEN @EventYear IS NOT NULL THEN @EventYear ELSE EventYear END) AND CertificateNumber = (CASE WHEN @CertificateNumber IS NOT NULL THEN @CertificateNumber ELSE CertificateNumber END) AND EventType = (CASE WHEN @EventType IS NOT NULL THEN @EventType ELSE EventType END) AND IGVersion = @IGVersion AND CreatedDate >= @Since ORDER BY CreatedDate OFFSET @Skip ROWS FETCH NEXT @Count ROWS ONLY END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250626190108_SelectOutgoingMessageItemsWithParams', N'7.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE PROCEDURE [dbo].[UpdateOutgoingMessagesRetrievedAt] @Id int, @RetrievedAt datetime AS BEGIN SET NOCOUNT ON UPDATE OutgoingMessageItems SET RetrievedAt = @RetrievedAt WHERE Id = @Id END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250626190143_UpdateRetrievedAtFields', N'7.0.0');
GO

COMMIT;
GO


