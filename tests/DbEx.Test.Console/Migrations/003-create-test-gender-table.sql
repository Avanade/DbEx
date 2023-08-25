    CREATE TABLE [Test].[Gender] (
      [GenderId] INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
      [Code] NVARCHAR (50) NOT NULL UNIQUE,
      [Text] VARCHAR (256) NOT NULL,
      [CreatedBy] NVARCHAR (50) NULL,
      [CreatedDate] DATETIME2 NULL,
      [UpdatedBy] NVARCHAR (50) NULL,
      [UpdatedDate] DATETIME2 NULL
    )