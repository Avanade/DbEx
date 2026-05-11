    CREATE TABLE `multi_pk` (
      `part1` INT NOT NULL,
      `part2` INT NOT NULL,
      `value` DECIMAL(16,4) NULL,
      `parts` INT GENERATED ALWAYS AS (`part1` + `part2`),
      CONSTRAINT PRIMARY KEY (`part1`, `part2`)
    )