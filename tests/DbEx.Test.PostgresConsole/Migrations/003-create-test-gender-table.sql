    CREATE TABLE "gender" (
      "gender_id" SERIAL PRIMARY KEY,
      "code" VARCHAR (50) NOT NULL UNIQUE,
      "text" VARCHAR (256) NOT NULL,
      "created_by" VARCHAR (50) NULL,
      "created_date" TIMESTAMPTZ NULL,
      "updated_by" VARCHAR (50) NULL,
      "updated_date" TIMESTAMPTZ NULL
    )