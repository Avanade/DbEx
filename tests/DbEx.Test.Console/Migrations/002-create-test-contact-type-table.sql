    CREATE TABLE [Test].[ContactType] (
      [ContactTypeId] INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
      [Code] NVARCHAR (50) NOT NULL UNIQUE,
      [Text] VARCHAR (256) NOT NULL,
      [SortOrder] INT NOT NULL
    )