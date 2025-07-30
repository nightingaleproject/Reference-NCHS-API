BEGIN TRANSACTION;
GO

ALTER TABLE [OutgoingMessageItems] ADD [IGVersion] CHAR(20) NULL;
GO

ALTER TABLE [IncomingMessageItems] ADD [IGVersion] CHAR(20) NULL;
GO

UPDATE OutgoingMessageItems SET IGVersion = 'VRDR_STU2_2' WHERE EventType = 'MOR'
GO

UPDATE IncomingMessageItems SET IGVersion = 'VRDR_STU2_2' WHERE EventType = 'MOR'
GO

UPDATE OutgoingMessageItems SET IGVersion = 'BFDR_STU2_0' WHERE EventType = 'NAT' or EventType = 'FET'
GO

UPDATE IncomingMessageItems SET IGVersion = 'BFDR_STU2_0' WHERE EventType = 'NAT' or EventType = 'FET'
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250512160614_IGVersion', N'6.0.36');
GO

COMMIT;
GO