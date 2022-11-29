    CREATE TABLE `contact` (
      `contact_id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
      `name` VARCHAR (200) NOT NULL,
      `phone` VARCHAR (15) NULL,
      `date_of_birth` DATE NULL,
      `contact_type_id` INT NOT NULL DEFAULT 1,
      `gender_id` INT NULL,
      CONSTRAINT `FK_Test_Contact_ContactType` FOREIGN KEY (`contact_type_id`) REFERENCES `contact_type` (`contact_type_id`)
    )