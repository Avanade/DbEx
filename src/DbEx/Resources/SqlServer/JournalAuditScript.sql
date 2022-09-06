-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-sqlserver/SqlTableJournal.cs for consistency.
INSERT INTO [dbo].[SchemaVersions] (ScriptName, Applied) values (@scriptName, @applied)