CREATE PROCEDURE [Test].[spGetContact]
  @ContactId AS INT
AS
BEGIN
  SET NOCOUNT ON;
  SELECT * FROM [Test].[Contact] AS [c] WHERE [c].[ContactId] = @ContactId
END