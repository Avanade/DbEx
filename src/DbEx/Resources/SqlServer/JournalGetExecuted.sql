-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-sqlserver/SqlTableJournal.cs for consistency.
SELECT DISTINCT [ScriptName] FROM [dbo].[SchemaVersions]