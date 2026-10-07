CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS hstore;
CREATE EXTENSION IF NOT EXISTS ltree;
CREATE EXTENSION IF NOT EXISTS citext;

CREATE TYPE "public"."mood" AS ENUM ('sad', 'ok', 'happy');

CREATE TABLE "public"."extra_types" (
  "extra_types_id" INT NOT NULL PRIMARY KEY,
  "ip_address" INET NULL,
  "network" CIDR NULL,
  "mac_address" MACADDR NULL,
  "mac_address8" MACADDR8 NULL,
  "search_vector" TSVECTOR NULL,
  "search_query" TSQUERY NULL,
  "pt" POINT NULL,
  "ln" LINE NULL,
  "seg" LSEG NULL,
  "bx" BOX NULL,
  "pth" PATH NULL,
  "poly" POLYGON NULL,
  "circ" CIRCLE NULL,
  "bit_flag" BIT(1) NULL,
  "bit_mask" BIT(8) NULL,
  "bit_varying" VARBIT(16) NULL,
  "tags" TEXT[] NULL,
  "numbers" INT[] NULL,
  "current_mood" "public"."mood" NULL,
  "moods" "public"."mood"[] NULL,
  "int_range" INT4RANGE NULL,
  "ts_range" TSTZRANGE NULL,
  "date_range" DATERANGE NULL,
  "int_multirange" INT4MULTIRANGE NULL,
  "attributes" HSTORE NULL,
  "label" LTREE NULL,
  "ci_text" CITEXT NULL,
  "geom" GEOMETRY(Point, 4326) NULL,
  "geom_any" GEOMETRY NULL,
  "geog" GEOGRAPHY NULL,
  "embedding" VECTOR(3) NULL,
  "half_embedding" HALFVEC(3) NULL,
  "sparse_embedding" SPARSEVEC(3) NULL
)
