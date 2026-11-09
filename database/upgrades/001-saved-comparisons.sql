SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.Clients', 'U') IS NULL OR OBJECT_ID('dbo.Properties', 'U') IS NULL OR OBJECT_ID('dbo.Quotes', 'U') IS NULL
    THROW 50001, 'Initialize the complete base schema before upgrading.', 1;

IF (COL_LENGTH('dbo.Properties', 'QuoteReference') IS NULL AND COL_LENGTH('dbo.Properties', 'CreatedAt') IS NOT NULL)
 OR (COL_LENGTH('dbo.Properties', 'QuoteReference') IS NOT NULL AND COL_LENGTH('dbo.Properties', 'CreatedAt') IS NULL)
    THROW 50002, 'Incomplete saved comparison upgrade; resolve the schema first.', 1;

IF COL_LENGTH('dbo.Properties', 'QuoteReference') IS NULL
BEGIN
    ALTER TABLE dbo.Properties ADD QuoteReference UNIQUEIDENTIFIER NULL, CreatedAt DATETIME2(0) NULL;
    -- Dynamic SQL compiles after the columns exist in an upgraded database.
    EXEC(N'UPDATE p SET QuoteReference = NEWID(), CreatedAt = c.CreatedAt
        FROM dbo.Properties p JOIN dbo.Clients c ON c.ClientID = p.ClientID;
        ALTER TABLE dbo.Properties ALTER COLUMN QuoteReference UNIQUEIDENTIFIER NOT NULL;
        ALTER TABLE dbo.Properties ALTER COLUMN CreatedAt DATETIME2(0) NOT NULL;
        ALTER TABLE dbo.Properties ADD CONSTRAINT DF_Properties_QuoteReference DEFAULT NEWID() FOR QuoteReference;
        ALTER TABLE dbo.Properties ADD CONSTRAINT DF_Properties_CreatedAt DEFAULT SYSUTCDATETIME() FOR CreatedAt;');
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Properties') AND name = 'UX_Properties_QuoteReference')
    EXEC(N'CREATE UNIQUE INDEX UX_Properties_QuoteReference ON dbo.Properties (QuoteReference);');

COMMIT TRANSACTION;
