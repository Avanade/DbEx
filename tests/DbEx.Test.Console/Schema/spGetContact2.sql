CREATE OR ALTER PROCEDURE [Test].[spGetContact2]
  @ContactId AS INT /* this is a comment */
AS
BEGIN
  -- This is a comment.
  SET NOCOUNT ON;
  SELECT * FROM [Test].[Contact] AS [c] WHERE [c].[ContactId] = @ContactId
END