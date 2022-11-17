-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-sqlserver/SqlTableJournal.cs for consistency.
SELECT DISTINCT [ScriptName] AS [scriptname] FROM [{{JournalSchema}}].[{{JournalTable}}]