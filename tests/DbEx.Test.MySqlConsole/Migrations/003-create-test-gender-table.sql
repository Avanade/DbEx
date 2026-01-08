    CREATE TABLE `gender` (
      `gender_id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
      `code` VARCHAR (50) NOT NULL UNIQUE,
      `text` VARCHAR (256) NOT NULL,
      `created_by` VARCHAR (50) NULL,
      `created_on` DATETIME(6) NULL,
      `updated_by` VARCHAR (50) NULL,
      `updated_on` DATETIME(6) NULL
    )