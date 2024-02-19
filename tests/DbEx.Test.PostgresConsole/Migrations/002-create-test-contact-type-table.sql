    CREATE TABLE "contact_type" (
      "contact_type_id" SERIAL PRIMARY KEY,
      "code" VARCHAR (50) NOT NULL UNIQUE,
      "text" VARCHAR (256) NOT NULL,
      "sort_order" INT NULL
    )