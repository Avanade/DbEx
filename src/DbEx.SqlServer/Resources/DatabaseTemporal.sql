SELECT SCHEMA_NAME(T2.schema_id) AS [schema], T2.name AS [table]
  FROM sys.tables T1  
    OUTER APPLY (SELECT is_temporal_history_retention_enabled FROM sys.databases
    WHERE NAME = DB_NAME()) AS DB
  LEFT JOIN sys.tables T2   
    ON T1.history_table_id = T2.object_id WHERE T1.temporal_type = 2