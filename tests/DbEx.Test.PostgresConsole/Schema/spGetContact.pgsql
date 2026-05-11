CREATE OR REPLACE PROCEDURE "spGetContact" (
  IN "contact_id" INT /* this is a comment */
)
LANGUAGE SQL
AS $$
  -- This is a comment.
  SELECT * FROM "contact" AS "c" WHERE "c"."contact_id" = "contact_id"
$$;