    CREATE TABLE `contact_type` (
      `contact_type_id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
      `code` VARCHAR (50) NOT NULL UNIQUE,
      `text` VARCHAR (256) NOT NULL,
      `sort_order` INT NULL
    )