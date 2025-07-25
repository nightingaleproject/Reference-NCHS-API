using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace messaging.Migrations
{
    /// <inheritdoc />
    public partial class StatusStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
						var createGetOverallStatusWithParamsProcedure = @"
CREATE PROCEDURE [dbo].[GetOverallStatusWithParams]
    @Since datetime2(7),
    @FiveMinutesAgo datetime2(7),
    @OneHourAgo datetime2(7)
AS
BEGIN

SELECT TOP(1) COUNT(CASE
    WHEN [t].[ProcessedStatus] = 'PROCESSED' THEN 1
END) AS [ProcessedCount], COUNT(CASE
    WHEN [t].[ProcessedStatus] = 'QUEUED' THEN 1
END) AS [QueuedCount], COALESCE((
    SELECT TOP(1) [t0].[CreatedDate]
    FROM (
        SELECT [i0].[Id], [i0].[CertificateNumber], [i0].[CreatedDate], [i0].[EventType], [i0].[EventYear], [i0].[IGVersion], [i0].[JurisdictionId], [i0].[Message], [i0].[MessageId], [i0].[MessageType], [i0].[ProcessedStatus], [i0].[Source], [i0].[UpdatedDate], 1 AS [Key]
        FROM [IncomingMessageItems] AS [i0]
        WHERE [i0].[CreatedDate] >= @Since
    ) AS [t0]
    WHERE [t].[Key] = [t0].[Key] AND [t0].[ProcessedStatus] = 'QUEUED'
    ORDER BY [t0].[CreatedDate]), '0001-01-01T00:00:00.0000000') AS [OldestQueued], COALESCE((
    SELECT TOP(1) [t1].[CreatedDate]
    FROM (
        SELECT [i1].[Id], [i1].[CertificateNumber], [i1].[CreatedDate], [i1].[EventType], [i1].[EventYear], [i1].[IGVersion], [i1].[JurisdictionId], [i1].[Message], [i1].[MessageId], [i1].[MessageType], [i1].[ProcessedStatus], [i1].[Source], [i1].[UpdatedDate], 1 AS [Key]
        FROM [IncomingMessageItems] AS [i1]
        WHERE [i1].[CreatedDate] >= @Since
    ) AS [t1]
    WHERE [t].[Key] = [t1].[Key] AND [t1].[ProcessedStatus] = 'QUEUED'
    ORDER BY [t1].[CreatedDate] DESC), '0001-01-01T00:00:00.0000000') AS [NewestQueued], COALESCE((
    SELECT TOP(1) [t2].[UpdatedDate]
    FROM (
        SELECT [i2].[Id], [i2].[CertificateNumber], [i2].[CreatedDate], [i2].[EventType], [i2].[EventYear], [i2].[IGVersion], [i2].[JurisdictionId], [i2].[Message], [i2].[MessageId], [i2].[MessageType], [i2].[ProcessedStatus], [i2].[Source], [i2].[UpdatedDate], 1 AS [Key]
        FROM [IncomingMessageItems] AS [i2]
        WHERE [i2].[CreatedDate] >= @Since
    ) AS [t2]
    WHERE [t].[Key] = [t2].[Key] AND [t2].[ProcessedStatus] = 'PROCESSED'
    ORDER BY [t2].[UpdatedDate] DESC), '0001-01-01T00:00:00.0000000') AS [LatestProcessed], COUNT(CASE
    WHEN [t].[ProcessedStatus] = 'PROCESSED' AND [t].[UpdatedDate] >= @FiveMinutesAgo THEN 1
END) AS [ProcessedCountFiveMinutes], COUNT(CASE
    WHEN [t].[ProcessedStatus] = 'PROCESSED' AND [t].[UpdatedDate] >= @OneHourAgo THEN 1
END) AS [ProcessedCountOneHour], COUNT(CASE
    WHEN [t].[ProcessedStatus] = 'QUEUED' AND [t].[CreatedDate] >= @FiveMinutesAgo THEN 1
END) AS [QueuedCountFiveMinutes], COUNT(CASE
    WHEN [t].[ProcessedStatus] = 'QUEUED' AND [t].[CreatedDate] >= @OneHourAgo THEN 1
END) AS [QueuedCountOneHour]
FROM (
    SELECT [i].[CreatedDate], [i].[ProcessedStatus], [i].[UpdatedDate], 1 AS [Key]
    FROM [IncomingMessageItems] AS [i]
    WHERE [i].[CreatedDate] >= @Since
) AS [t]
GROUP BY [t].[Key]

END
";
				    migrationBuilder.Sql(createGetOverallStatusWithParamsProcedure);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
						string[] procedureNames = {"GetOverallStatusWithParams"};
						foreach(string procedureName in procedureNames)
						{
								var dropProcedure = $"DROP PROCEDURE [dbo].[{procedureName}]";
								migrationBuilder.Sql(dropProcedure);
						}
        }
    }
}
