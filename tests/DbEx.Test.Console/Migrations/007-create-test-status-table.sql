CREATE TABLE [Test].[Status] (
    [StatusId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()) PRIMARY KEY,
    [Code] NVARCHAR (50) NOT NULL,
    [Text] VARCHAR (256) NOT NULL,
    [CreatedBy] NVARCHAR (50) NULL,
    [CreatedDate] DATETIME2 NULL,
    [UpdatedBy] NVARCHAR (50) NULL,
    [UpdatedDate] DATETIME2 NULL,
    [TenantId] NVARCHAR(50) NOT NULL,
    UNIQUE ([TenantId], [Code])
)