IF (DB_ID('@DatabaseName') IS NULL)
BEGIN
  SELECT 'Database ''@DatabaseName'' does not exist and therefore not dropped.'
END
ELSE
BEGIN
  ALTER DATABASE [@DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [@DatabaseName];
  SELECT 'Database ''@DatabaseName'' dropped.'
END