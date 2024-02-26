﻿-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

SELECT *
   FROM INFORMATION_SCHEMA.TABLES as t
     INNER JOIN INFORMATION_SCHEMA.COLUMNS as c
	   ON t.TABLE_CATALOG = c.TABLE_CATALOG
		 AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
		 AND t.TABLE_NAME = c.TABLE_NAME
   WHERE t.TABLE_CATALOG = '{{DatabaseName}}'
     AND t.TABLE_SCHEMA <> 'information_schema'
     AND t.TABLE_SCHEMA <> 'pg_catalog'
   ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION