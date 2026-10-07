-- Create table: [Test].[ExtraTypes] - the "extra" (non-primitive) types; the same shape is used for all providers.

BEGIN TRANSACTION

CREATE TABLE [Test].[ExtraTypes] (
  [ExtraTypesId] INT NOT NULL PRIMARY KEY,
  [Location] GEOGRAPHY NULL,
  [Shape] GEOMETRY NULL,
  [Path] HIERARCHYID NULL,
  [Document] XML NULL,
  [Embedding] VECTOR(3) NULL,
  [Variant] SQL_VARIANT NULL,
  [Payload] JSON NULL
)

COMMIT TRANSACTION
