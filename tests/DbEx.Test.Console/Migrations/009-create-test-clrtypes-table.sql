-- Create table: [Test].[ClrTypes]

BEGIN TRANSACTION

CREATE TABLE [Test].[ClrTypes] (
  [ClrTypesId] INT NOT NULL PRIMARY KEY,
  [Location] GEOGRAPHY NULL,
  [Shape] GEOMETRY NULL,
  [Path] HIERARCHYID NULL,
  [Document] XML NULL
)
	
COMMIT TRANSACTION
