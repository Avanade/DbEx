-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/Beef

CREATE OR REPLACE FUNCTION fn_get_timestamp(
  "Override" TIMESTAMP WITH TIME ZONE = NULL
)
RETURNS TIMESTAMP WITH TIME ZONE
LANGUAGE plpgsql
AS $$
DECLARE "Timestamp" TIMESTAMP WITH TIME ZONE;
BEGIN
  "Timestamp" := CURRENT_TIMESTAMP;
  IF "Override" IS NULL THEN
 	"Timestamp" := to_timestamp(current_setting('Session.Timestamp', true), 'YYYY-MM-DD"T"HH24:MI:SS.FF6');
 	IF "Timestamp" IS NULL THEN
 	  "Timestamp" := CURRENT_TIMESTAMP;
 	END IF;
  ELSE
 	"Timestamp" := "Override";
  END IF;

  RETURN "Timestamp";
END
$$;