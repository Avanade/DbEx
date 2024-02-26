    CREATE TABLE "multi_pk" (
      "part1" INT NOT NULL,
      "part2" INT NOT NULL,
      "value" money NULL,
      "parts" INT GENERATED ALWAYS AS ("part1" + "part2") STORED,
      PRIMARY KEY ("part1", "part2")
    )