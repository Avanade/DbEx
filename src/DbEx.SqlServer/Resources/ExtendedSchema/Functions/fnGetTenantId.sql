-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/Beef

CREATE OR ALTER FUNCTION [dbo].[fnGetTenantId]
(
  @Override as NVARCHAR(1024) = null
)
RETURNS NVARCHAR(1024)
AS
BEGIN
  DECLARE @TenantId NVARCHAR(1024)
  IF @Override IS NULL
  BEGIN
    SET @TenantId = CONVERT(NVARCHAR(1024), SESSION_CONTEXT(N'TenantId'));
  END
  ELSE
  BEGIN
    SET @TenantId = @Override
  END
  
  RETURN @TenantId
END