-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/Beef

CREATE OR REPLACE FUNCTION fn_get_user_id(
  "Override" TEXT = NULL
)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE "UserId" TEXT;
BEGIN
  IF "Override" IS NULL THEN
 	"UserId" := current_setting('Session.UserId', true);
  ELSE
 	"UserId" := "Override";
  END IF;

  RETURN "UserId";
END
$$;