IF (DB_ID('@DatabaseName') IS NULL)
BEGIN
  CREATE DATABASE [@DatabaseName]
  SELECT 'Database ''@DatabaseName'' did not exist and was created.'
END
ELSE
BEGIN
  SELECT 'Database ''@DatabaseName'' already exists and therefore not created.'
END