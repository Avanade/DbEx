-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

CREATE OR ALTER FUNCTION [dbo].[fnGetTimestamp]
(
  @Override AS DATETIMEOFFSET = NULL	
)
RETURNS DATETIMEOFFSET
AS
BEGIN
  DECLARE @Timestamp DATETIMEOFFSET
  IF @Override IS NULL
  BEGIN
    SET @Timestamp = CONVERT(DATETIMEOFFSET, SESSION_CONTEXT(N'Timestamp'));
    IF @Timestamp IS NULL
    BEGIN
      SET @Timestamp = SYSDATETIMEOFFSET()
    END
  END
  ELSE
  BEGIN
    SET @Timestamp = @Override
  END
  
  RETURN @Timestamp
END