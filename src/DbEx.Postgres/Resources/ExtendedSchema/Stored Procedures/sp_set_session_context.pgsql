-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

CREATE OR REPLACE PROCEDURE sp_set_session_context(
  "Timestamp" TIMESTAMP WITH TIME ZONE = NULL,
  "Username" TEXT = NULL,
  "TenantId" TEXT = NULL,
  "UserId" TEXT = NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
  IF "Timestamp" IS NOT NULL THEN
	PERFORM set_config('Session.Timestamp', to_char("Timestamp", 'YYYY-MM-DD"T"HH24:MI:SS.FF6'), false);
  END IF;
  
  IF "Username" IS NOT NULL THEN
	PERFORM set_config('Session.Username', "Username", false);
  END IF;
  
  IF "TenantId" IS NOT NULL THEN
	PERFORM set_config('Session.TenantId', "TenantId", false);
  END IF;
  
  IF "UserId" IS NOT NULL THEN
	PERFORM set_config('Session.UserId', "UserId", false);
  END IF;
END
$$;