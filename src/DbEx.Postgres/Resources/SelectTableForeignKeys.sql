-- Inspired by: https://stackoverflow.com/a/52130270
SELECT
    tc.constraint_name, tc.table_catalog, tc.table_schema, tc.table_name, kcu.column_name, 
    ccu.table_schema AS foreign_schema_name, ccu.table_name AS foreign_table_name, ccu.column_name AS foreign_column_name 
  FROM information_schema.table_constraints AS tc 
  JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
  JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
 WHERE constraint_type = 'FOREIGN KEY'
   AND tc.table_catalog = '{{DatabaseName}}'
   AND ccu.table_catalog = '{{DatabaseName}}'