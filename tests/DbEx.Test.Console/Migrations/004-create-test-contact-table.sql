    CREATE TABLE [Test].[Contact] (
      [ContactId] INT NOT NULL PRIMARY KEY,
      [Name] NVARCHAR (200) NOT NULL,
      [Phone] VARCHAR (15) NULL,
      [DateOfBirth] DATE NULL,
      [ContactTypeId] INT NOT NULL DEFAULT 1,
      [GenderId] INT NULL,
      [TenantId] NVARCHAR(50),
      [Notes] NVARCHAR(MAX) NULL,
      CONSTRAINT [FK_Test_Contact_ContactType] FOREIGN KEY ([ContactTypeId]) REFERENCES [Test].[ContactType] ([ContactTypeId])
    )