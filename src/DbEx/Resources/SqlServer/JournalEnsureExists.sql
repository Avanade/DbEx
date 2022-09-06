-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-sqlserver/SqlTableJournal.cs for consistency.
IF (OBJECT_ID(N'dbo.SchemaVersions') IS NULL)
BEGIN
  CREATE TABLE [dbo].[SchemaVersions] (
    [Id] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
    [ScriptName] NVARCHAR(255) NOT NULL,
    [Applied] DATETIME2 NOT NULL)

  SELECT '*Journal table [dbo].[SchemaVersion] did not exist within the database and was automatically created.'
END