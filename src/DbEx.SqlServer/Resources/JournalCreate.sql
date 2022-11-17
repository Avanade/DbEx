-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-sqlserver/SqlTableJournal.cs for consistency.
CREATE TABLE [{{JournalSchema}}].[{{JournalTable}}] (
  [Id] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
  [ScriptName] NVARCHAR(255) NOT NULL,
  [Applied] DATETIME2 NOT NULL)