﻿-- Inspired by https://github.com/DbUp/dbup-postgresql/blob/main/src/dbup-postgresql/PostgresqlTableJournal.cs for consistency.
INSERT INTO "{{JournalSchema}}"."{{JournalTable}}" (ScriptName, Applied) VALUES (@scriptname, @applied)