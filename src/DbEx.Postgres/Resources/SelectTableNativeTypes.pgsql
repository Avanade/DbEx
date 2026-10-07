SELECT n.nspname AS table_schema, c.relname AS table_name, a.attname AS column_name,
       format_type(a.atttypid, a.atttypmod) AS native_type,
       t.typname::text AS udt_name, t.typtype::text AS type_kind,
       CASE WHEN t.typcategory = 'A' THEN et.typname::text END AS element_udt_name,
       CASE WHEN t.typcategory = 'A' THEN et.typtype::text END AS element_type_kind
  FROM pg_catalog.pg_attribute AS a
    INNER JOIN pg_catalog.pg_class AS c ON c.oid = a.attrelid
    INNER JOIN pg_catalog.pg_namespace AS n ON n.oid = c.relnamespace
    INNER JOIN pg_catalog.pg_type AS t ON t.oid = a.atttypid
    LEFT OUTER JOIN pg_catalog.pg_type AS et ON et.oid = t.typelem
 WHERE a.attnum > 0 AND NOT a.attisdropped AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
   AND n.nspname NOT IN ('information_schema', 'pg_catalog')
