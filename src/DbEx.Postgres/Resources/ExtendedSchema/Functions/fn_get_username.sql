-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/Beef

CREATE OR REPLACE FUNCTION fn_get_username(
  "Override" TEXT = NULL
)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE "Username" TEXT;
BEGIN
  IF "Override" IS NULL THEN
 	"Username" := current_setting('Session.Username', true);
    IF "Username" IS NULL THEN
	  "Username" := current_user;
	END IF;
  ELSE
 	"Username" := "Override";
  END IF;

  RETURN "Username";
END
$$;