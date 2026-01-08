    CREATE TABLE [Test].[Gender] (
      [GenderId] INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
      [Code] NVARCHAR (50) NOT NULL UNIQUE,
      [Text] VARCHAR (256) NOT NULL,
      [CreatedBy] NVARCHAR(250) NULL,
      [CreatedOn] DATETIMEOFFSET NULL,
      [UpdatedBy] NVARCHAR(250) NULL,
      [UpdatedOn] DATETIMEOFFSET NULL
    )