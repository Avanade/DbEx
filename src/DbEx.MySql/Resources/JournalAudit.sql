-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-mysql/MySqlTableJournal.cs for consistency.
INSERT INTO `{{JournalTable}}` (scriptname, applied) values (@scriptname, @applied)