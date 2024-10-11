-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

CREATE OR ALTER PROCEDURE [dbo].[spSetSessionContext]
  @Timestamp DATETIME2 = null,
  @Username NVARCHAR(1024) = null,
  @TenantId NVARCHAR(1024) = null,
  @UserId NVARCHAR(1024) = null
AS
BEGIN
  IF @Timestamp IS NOT NULL
  BEGIN
    EXEC sp_set_session_context 'Timestamp', @Timestamp, @read_only = 1;
  END
  
  IF @Username IS NOT NULL
  BEGIN
    EXEC sp_set_session_context 'Username', @Username, @read_only = 1;
  END
  
  IF @TenantId IS NOT NULL
  BEGIN
    EXEC sp_set_session_context 'TenantId', @TenantId, @read_only = 1;
  END
  
  IF @UserId IS NOT NULL
  BEGIN
    EXEC sp_set_session_context 'UserId', @UserId, @read_only = 1;
  END
END