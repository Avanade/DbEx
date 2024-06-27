    CREATE TABLE [Test].[Person] (
      [PersonId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()) PRIMARY KEY,
      [Name] NVARCHAR (200) NOT NULL,
      [NicknamesJson] NVARCHAR (500) NULL,
      [AddressJson] NVARCHAR (500) NULL,
      [CreatedBy] NVARCHAR (200) NULL,
      [CreatedDate] DATETIME2 NULL,
      [UpdatedBy] NVARCHAR (200) NULL,
      [UpdatedDate] DATETIME2 NULL
    )