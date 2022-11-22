-- Inspired by https://github.com/DbUp/DbUp/blob/master/src/dbup-mysql/MySqlTableJournal.cs for consistency.
CREATE TABLE `{{JournalTable}}` (
  `schemaversionid` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `scriptname` VARCHAR(255) NOT NULL,
  `applied` DATETIME(6) NOT NULL)