    CREATE TABLE [Test].[ContactAddress] (
      [ContactAddressId] INT NOT NULL PRIMARY KEY,
      [ContactId] INT NOT NULL,
      [Street] NVARCHAR (200) NOT NULL,
      CONSTRAINT [FK_Test_ContactAddress_Contact] FOREIGN KEY ([ContactId]) REFERENCES [Test].[Contact] ([ContactId])
    )