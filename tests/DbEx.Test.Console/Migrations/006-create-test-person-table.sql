    CREATE TABLE [Test].[Person] (
      [PersonId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()) PRIMARY KEY,
      [Name] NVARCHAR (200) NOT NULL,
      [NicknamesJson] NVARCHAR (500) NULL,
      [AddressJson] NVARCHAR (500) NULL,
      [CreatedBy] NVARCHAR(250) NULL,
      [CreatedOn] DATETIMEOFFSET NULL,
      [UpdatedBy] NVARCHAR(250) NULL,
      [UpdatedOn] DATETIMEOFFSET NULL
    )