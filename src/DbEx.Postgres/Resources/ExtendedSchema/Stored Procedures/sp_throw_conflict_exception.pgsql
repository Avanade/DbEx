-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

CREATE OR REPLACE PROCEDURE sp_throw_conflict_exception(
  "message" TEXT = NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
  IF "message" IS NULL THEN
    "message" := '';
  END IF;

  RAISE USING MESSAGE = "message", ERRCODE = '56006';
END
$$;