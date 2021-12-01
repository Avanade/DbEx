    CREATE TABLE [Test].[Person] (
      [PersonId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
      [Name] NVARCHAR (200) NOT NULL,
      [CreatedBy] NVARCHAR (200) NOT NULL,
      [CreatedDate] DATETIME2 NOT NULL
    )