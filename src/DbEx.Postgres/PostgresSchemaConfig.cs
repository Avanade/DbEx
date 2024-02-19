// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.DbSchema;
using DbEx.Migration.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DbEx.Migration;
using CoreEx;

namespace DbEx.Postgres
{
    /// <summary>
    /// Provides PostgreSQL specific configuration and capabilities.
    /// </summary>
    /// <param name="databaseName">The database name.</param>
    public class PostgresSchemaConfig(string databaseName) : DatabaseSchemaConfig(databaseName, true, "public")
    {
        /// <inheritdoc/>
        /// <remarks>Value is '<c>_id</c>'.</remarks>
        public override string IdColumnNameSuffix => "_id";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>created_date</c>'.</remarks>
        public override string CreatedDateColumnName => "created_date";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>created_by</c>'.</remarks>
        public override string CreatedByColumnName => "created_by";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>updated_date</c>'.</remarks>
        public override string UpdatedDateColumnName => "updated_date";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>updated_by</c>'.</remarks>
        public override string UpdatedByColumnName => "updated_by";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>tenant_id</c>'.</remarks>
        public override string TenantIdColumnName => "tenant_id";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>xmin</c>'. This is a PostgreSQL system column (hidden); see <see href="https://www.postgresql.org/docs/current/ddl-system-columns.html#DDL-SYSTEM-COLUMNS"/> 
        /// and <see href="https://www.npgsql.org/efcore/modeling/concurrency.html"/> for more information.</remarks>
        public override string RowVersionColumnName => "xmin";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>is_deleted</c>'.</remarks>
        public override string IsDeletedColumnName => "is_deleted";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>code</c>'.</remarks>
        public override string RefDataCodeColumnName => "code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>text</c>'.</remarks>
        public override string RefDataTextColumnName => "text";

        /// <inheritdoc/>
        public override string ToFullyQualifiedTableName(string? schema, string table) => string.IsNullOrEmpty(schema) ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";

        /// <inheritdoc/>
        public override void PrepareDataParserArgs(DataParserArgs dataParserArgs)
        {
            if (dataParserArgs == null)
                return;

            if (dataParserArgs.RefDataColumnDefaults.Count == 0)
            {
                dataParserArgs.RefDataColumnDefaults.TryAdd("is_active", _ => true);
                dataParserArgs.RefDataColumnDefaults.TryAdd("sort_order", i => i);
            }

            dataParserArgs.IdColumnNameSuffix ??= IdColumnNameSuffix;
            dataParserArgs.CreatedByColumnName ??= CreatedByColumnName;
            dataParserArgs.CreatedDateColumnName ??= CreatedDateColumnName;
            dataParserArgs.UpdatedByColumnName ??= UpdatedByColumnName;
            dataParserArgs.UpdatedDateColumnName ??= UpdatedDateColumnName;
            dataParserArgs.TenantIdColumnName ??= TenantIdColumnName;
            dataParserArgs.RowVersionColumnName ??= RowVersionColumnName;
            dataParserArgs.IsDeletedColumnName ??= IsDeletedColumnName;
            dataParserArgs.RefDataCodeColumnName ??= RefDataCodeColumnName;
            dataParserArgs.RefDataTextColumnName ??= RefDataTextColumnName;
        }

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr) => new(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
        {
            IsNullable = dr.GetValue<string>("IS_NULLABLE").Equals("YES", StringComparison.OrdinalIgnoreCase),
            Length = (ulong?)dr.GetValue<long?>("CHARACTER_MAXIMUM_LENGTH"),
            Precision = (ulong?)(dr.GetValue<int?>("NUMERIC_PRECISION") ?? dr.GetValue<int?>("DATETIME_PRECISION")),
            Scale = (ulong?)dr.GetValue<int?>("NUMERIC_SCALE"),
            DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT") is not null && dr.GetValue<string>("COLUMN_DEFAULT").StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ? null : dr.GetValue<string>("COLUMN_DEFAULT"),
            IsComputed = dr.GetValue<string?>("IS_GENERATED") != "NEVER",
            IsIdentity = dr.GetValue<string>("COLUMN_DEFAULT")?.StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ?? false
        };

        /// <inheritdoc/>
        public override async Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, DataParserArgs? dataParserArgs, CancellationToken cancellationToken)
        {
            // Add the row version 'xmin' column to the table schema.
            foreach (var table in tables)
            {
                table.Columns.Add(new DbColumnSchema(table, RowVersionColumnName, "xid", "RowVersion")
                {
                    IsNullable = false,
                    Scale = 0,
                    Precision = 32,
                    IsComputed = true,
                    IsRowVersion = true
                });
            }

            // Configure all the single column foreign keys.
            using var sr3 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableForeignKeys.sql", [typeof(PostgresSchemaConfig).Assembly]);
            var fks = await database.SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
            {
                ConstraintName = dr.GetValue<string>("constraint_name"),
                TableSchema = dr.GetValue<string>("table_schema"),
                TableName = dr.GetValue<string>("table_name"),
                TableColumnName = dr.GetValue<string>("column_name"),
                ForeignSchema = dr.GetValue<string>("foreign_schema_name"),
                ForeignTable = dr.GetValue<string>("foreign_table_name"),
                ForiegnColumn = dr.GetValue<string>("foreign_column_name")
            }, cancellationToken).ConfigureAwait(false);

            foreach (var grp in fks.GroupBy(x => new { x.ConstraintName, x.TableSchema, x.TableName }).Where(x => x.Count() == 1))
            {
                var fk = grp.Single();
                var r = (from t in tables
                         from c in t.Columns
                         where (t.Schema == fk.TableSchema && t.Name == fk.TableName && c.Name == fk.TableColumnName)
                         select (t, c)).SingleOrDefault();

                if (r == default)
                    continue;

                r.c.ForeignSchema = fk.ForeignSchema;
                r.c.ForeignTable = fk.ForeignTable;
                r.c.ForeignColumn = fk.ForiegnColumn;
                r.c.IsForeignRefData = (from t in tables where (t.Schema == fk.ForeignSchema && t.Name == fk.ForeignTable) select t.IsRefData).FirstOrDefault();
            }
        }

        /// <inheritdoc/>
        public override string ToDotNetTypeName(DbColumnSchema schema)
        {
            var dbType = schema.ThrowIfNull(nameof(schema)).Type;
            if (string.IsNullOrEmpty(dbType))
                return "string";

            // Source of truth: https://www.npgsql.org/doc/types/basic.html
            return dbType.ToUpperInvariant() switch
            {
                "TEXT" or "CHARACTER VARYING" or "CHARACTER" or "CITEXT" or "JSON" or "JSONB" or "XML" or "NAME" => "string",
                "NUMERIC" or "MONEY" => "decimal",
                "TIMESTAMP WITHOUT TIME ZONE" or "TIMESTAMP WITH TIME ZONE" or "DATE" => "DateTime",
                "BYTEA" => "byte[]",
                "BOOLEAN" or "BIT(1)" => "bool",
                "TIME WITH TIME ZONE" => "DateTimeOffset",
                "DOUBLE PRECISION" => "double",
                "INTEGER" => "int",
                "BIGINT" => "long",
                "SMALLINT" => "short",
                "REAL" => "float",
                "TIME WITHOUT TIME ZONE" or "INTERVAL" => "TimeSpan",
                "UUID" => "Guid",
                "XID" => "uint",
                _ => throw new InvalidOperationException($"Database data type '{dbType}' does not have corresponding .NET type mapping defined."),
            };
        }

        /// <inheritdoc/>
        public override string ToFormattedSqlType(DbColumnSchema schema, bool includeNullability = true)
        {
            var sb = new StringBuilder(schema.Type!.ToUpperInvariant());
            
            sb.Append(schema.Type.ToUpperInvariant() switch
            {
                "CHARACTER VARYING" or "CHARACTER" => schema.Length.HasValue && schema.Length.Value > 0 ? $"({schema.Length.Value})" : "(MAX)",
                "NUMERIC" => $"({schema.Precision}, {schema.Scale})",
                "TIMESTAMP WITHOUT TIME ZONE" or "TIMESTAMP WITH TIME ZONE" or "TIME WITH TIME ZONE" or "TIME WITHOUT TIME ZONE" => schema.Scale.HasValue && schema.Scale.Value > 0 ? $"({schema.Scale})" : string.Empty,
                _ => string.Empty
            });

            if (includeNullability && schema.IsNullable)
                sb.Append(" NULL");

            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToFormattedSqlStatementValue(DbColumnSchema dbColumnSchema, DataParserArgs dataParserArgs, object? value) => value switch
        {
            null => "NULL",
            string str => $"'{str.Replace("'", "''", StringComparison.Ordinal)}'",
            bool b => b ? "true" : "false",
            Guid => $"'{value}'",
            DateTime dt => $"'{dt.ToString(dataParserArgs.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
            DateTimeOffset dto => $"'{dto.ToString(dataParserArgs.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
            _ => value.ToString()!
        };

        /// <inheritdoc/>
        public override bool IsDbTypeInteger(string? dbType) => dbType != null && dbType.ToUpperInvariant() switch
        {
            "INTEGER" or "BIGINT" or "SMALLINT" or "XID" => true,
            _ => false
        };

        /// <inheritdoc/>
        public override bool IsDbTypeDecimal(string? dbType) => dbType != null && dbType.ToUpperInvariant() switch
        {
            "NUMERIC" or "MONEY" => true,
            _ => false
        };

        /// <inheritdoc/>
        public override bool IsDbTypeString(string? dbType) => dbType != null && dbType.ToUpperInvariant() switch
        {
            "TEXT" or "CHARACTER VARYING" or "CHARACTER" or "CITEXT" or "JSON" or "JSONB" or "XML" or "NAME" => true,
            _ => false
        };
    }
}