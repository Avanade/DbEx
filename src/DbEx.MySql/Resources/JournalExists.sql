-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-mysql/MySqlTableJournal.cs for consistency.
SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{{JournalTable}}'