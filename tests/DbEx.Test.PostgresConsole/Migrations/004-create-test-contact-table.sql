    CREATE TABLE "public"."contact" (
      "contact_id" SERIAL PRIMARY KEY,
      "name" VARCHAR (200) NOT NULL,
      "phone" VARCHAR (15) NULL,
      "date_of_birth" DATE NULL,
      "contact_type_id" INT NOT NULL DEFAULT 1,
      "gender_id" INT NULL,
      "notes" TEXT NULL,
      "created_by" VARCHAR (50) NULL,
      "created_date" TIMESTAMPTZ NULL,
      "updated_by" VARCHAR (50) NULL,
      "updated_date" TIMESTAMPTZ NULL,
      "contact_type_code" VARCHAR(50) NULL,
      CONSTRAINT "FK_Test_Contact_ContactType" FOREIGN KEY ("contact_type_id") REFERENCES "contact_type" ("contact_type_id")
    )