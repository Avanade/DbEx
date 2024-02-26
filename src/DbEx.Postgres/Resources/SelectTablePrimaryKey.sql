SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE, kcu.COLUMN_NAME, kcu.ORDINAL_POSITION
  FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
  JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
   AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
   AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
   AND kcu.TABLE_NAME = tc.TABLE_NAME
 WHERE kcu.TABLE_CATALOG = '{{DatabaseName}}'
   AND kcu.TABLE_SCHEMA NOT IN ('information_schema', 'pg_catalog')
   AND tc.CONSTRAINT_TYPE IN ( 'PRIMARY KEY', 'UNIQUE' )
 ORDER BY kcu.TABLE_SCHEMA, kcu.TABLE_NAME, tc.CONSTRAINT_TYPE, kcu.CONSTRAINT_NAME, kcu.ORDINAL_POSITION;