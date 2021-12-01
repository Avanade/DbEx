    CREATE TABLE [Test].[MultiPk] (
      [Part1] INT NOT NULL,
      [Part2] INT NOT NULL,
      [Value] DECIMAL(16,4) NULL,
      [Parts] AS (CAST(Part1 AS NVARCHAR(32)) + '.' + CAST(Part2 AS NVARCHAR(32)))
      CONSTRAINT [PK_Test_MultiPk] PRIMARY KEY ([Part1], [Part2])
    )