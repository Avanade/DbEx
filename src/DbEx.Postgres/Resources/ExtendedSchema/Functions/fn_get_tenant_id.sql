-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/Beef

CREATE OR REPLACE FUNCTION fn_get_tenant_id(
  "Override" TEXT = NULL
)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE "TenantId" TEXT;
BEGIN
  IF "Override" IS NULL THEN
 	"TenantId" := current_setting('Session.TenantId', true);
  ELSE
 	"TenantId" := "Override";
  END IF;

  RETURN "TenantId";
END
$$;