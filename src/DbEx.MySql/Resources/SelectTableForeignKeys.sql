-- Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

select fks.constraint_schema, fks.table_name, fks.unique_constraint_schema, fks.referenced_table_name, kcu.ordinal_position,
       kcu.column_name as fk_column_name, kcu.referenced_column_name as pk_column_name, fks.constraint_name as fk_constraint_name
from information_schema.referential_constraints fks
join information_schema.key_column_usage kcu
  on fks.constraint_schema = kcu.table_schema
  and fks.table_name = kcu.table_name
  and fks.constraint_name = kcu.constraint_name
where kcu.table_schema not in ('information_schema', 'sys', 'mysql', 'performance_schema')
order by fks.constraint_schema, fks.table_name, kcu.ordinal_position;