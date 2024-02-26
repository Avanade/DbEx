-- Inspired by https://github.com/DbUp/dbup-postgresql/blob/main/src/dbup-postgresql/PostgresqlTableJournal.cs for consistency.
SELECT DISTINCT "scriptname" FROM "{{JournalSchema}}"."{{JournalTable}}"