SELECT SCHEMA_NAME(o.schema_id) AS TABLE_SCHEMA, o.name AS TABLE_NAME, c.name AS COLUMN_NAME, c.vector_dimensions AS VECTOR_DIMENSIONS
  FROM sys.columns AS c
    INNER JOIN sys.objects AS o ON o.object_id = c.object_id
 WHERE c.vector_dimensions IS NOT NULL
