-- Inspired by https://github.com/DbUp/dbup-postgresql/blob/main/src/dbup-postgresql/PostgresqlTableJournal.cs for consistency.
CREATE TABLE "{{JournalSchema}}"."{{JournalTable}}" (
  schemaversionsid serial NOT NULL PRIMARY KEY,
  scriptname CHARACTER VARYING(255) NOT NULL,
  applied TIMESTAMP WITHOUT TIME ZONE NOT NULL)