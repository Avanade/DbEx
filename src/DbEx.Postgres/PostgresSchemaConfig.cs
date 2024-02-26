// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using CoreEx.Database;
using DbEx.DbSchema;
using DbEx.Migration;
using DbEx.Postgres.Migration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Postgres
{
    /// <summary>
    /// Provides PostgreSQL specific configuration and capabilities.
    /// </summary>
    /// <param name="migration">The owning <see cref="PostgresMigration"/>.</param>
    public class PostgresSchemaConfig(PostgresMigration migration) : DatabaseSchemaConfig(migration, true, "public")
    {
        /// <inheritdoc/>
        /// <remarks>Value is '<c>_id</c>'.</remarks>
        public override string IdColumnNameSuffix => "_id";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>_code</c>'.</remarks>
        public override string CodeColumnNameSuffix => "_code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>_json</c>'.</remarks>
        public override string JsonColumnNameSuffix => "_json";

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
        public override void PrepareMigrationArgs()
        {
            base.PrepareMigrationArgs();

            Migration.Args.DataParserArgs.RefDataColumnDefaults.TryAdd("is_active", _ => true);
            Migration.Args.DataParserArgs.RefDataColumnDefaults.TryAdd("sort_order", i => i);
        }

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr)
        {
            var c = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
            {
                IsNullable = dr.GetValue<string>("IS_NULLABLE").Equals("YES", StringComparison.OrdinalIgnoreCase),
                Length = (ulong?)dr.GetValue<long?>("CHARACTER_MAXIMUM_LENGTH"),
                Precision = (ulong?)(dr.GetValue<int?>("NUMERIC_PRECISION") ?? dr.GetValue<int?>("DATETIME_PRECISION")),
                Scale = (ulong?)dr.GetValue<int?>("NUMERIC_SCALE"),
                DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT") is not null && dr.GetValue<string>("COLUMN_DEFAULT").StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ? null : dr.GetValue<string>("COLUMN_DEFAULT"),
                IsComputed = dr.GetValue<string?>("IS_GENERATED") != "NEVER",
                IsIdentity = dr.GetValue<string>("COLUMN_DEFAULT")?.StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ?? false,
                IsDotNetDateOnly = RemovePrecisionFromDataType(dr.GetValue<string>("DATA_TYPE")).Equals("DATE", StringComparison.OrdinalIgnoreCase),
                IsDotNetTimeOnly = RemovePrecisionFromDataType(dr.GetValue<string>("DATA_TYPE")).Equals("TIME WITHOUT TIME ZONE", StringComparison.OrdinalIgnoreCase)
            };

            c.IsJsonContent = c.Type.ToUpper() == "JSON" || (c.DotNetName == "string" && c.Name.EndsWith(JsonColumnNameSuffix, StringComparison.Ordinal));
            if (c.IsJsonContent && c.Name.EndsWith(JsonColumnNameSuffix, StringComparison.Ordinal))
                c.DotNetCleanedName = DbTableSchema.CreateDotNetName(c.Name[..^JsonColumnNameSuffix.Length]);

            return c;
        }

        /// <summary>
        /// Removes any precision from the data type.
        /// </summary>
        private static string RemovePrecisionFromDataType(string type) => type.Contains('(') ? type[..type.IndexOf('(')] : type;

        /// <inheritdoc/>
        public override async Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, CancellationToken cancellationToken)
        {
            // Add the row version 'xmin' column to the table schema.
            foreach (var table in tables)
            {
                table.Columns.Add(new DbColumnSchema(table, migration.Args.RowVersionColumnName!, "xid", "RowVersion")
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
            var dbType = RemovePrecisionFromDataType(schema.ThrowIfNull(nameof(schema)).Type);
            if (string.IsNullOrEmpty(dbType))
                return "string";

            if (Migration.Args.EmitDotNetDateOnly && schema.IsDotNetDateOnly)
                return "DateOnly";
            else if (Migration.Args.EmitDotNetTimeOnly && schema.IsDotNetTimeOnly)
                return "TimeOnly";

            // Source of truth: https://www.npgsql.org/doc/types/basic.html
            return dbType.ToUpperInvariant() switch
            {
                "TEXT" or "CHARACTER VARYING" or "CHARACTER" or "CITEXT" or "JSON" or "JSONB" or "XML" or "NAME" => "string",
                "NUMERIC" or "MONEY" => "decimal",
                "TIMESTAMP WITHOUT TIME ZONE" or "TIMESTAMP WITH TIME ZONE" => "DateTime",
                "TIME WITH TIME ZONE" => "DateTimeOffset",
                "INTERVAL" => "TimeSpan",
                "TIME WITHOUT TIME ZONE" => "TimeSpan", // TimeOnly
                "DATE" => "DateTime", // DateOnly
                "BYTEA" => "byte[]",
                "BOOLEAN" or "BIT(1)" => "bool",
                "DOUBLE PRECISION" => "double",
                "INTEGER" => "int",
                "BIGINT" => "long",
                "SMALLINT" => "short",
                "REAL" => "float",
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
        public override string ToFormattedSqlStatementValue(DbColumnSchema dbColumnSchema, object? value) => value switch
        {
            null => "NULL",
            string str => $"'{str.Replace("'", "''", StringComparison.Ordinal)}'",
            bool b => b ? "true" : "false",
            Guid => $"uuid('{value}')",
            DateTime dt => $"'{dt.ToString(Migration.Args.DataParserArgs.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
            DateTimeOffset dto => $"'{dto.ToString(Migration.Args.DataParserArgs.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
#if NET7_0_OR_GREATER
            DateOnly d => $"'{d.ToString(Migration.Args.DataParserArgs.DateOnlyFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
            TimeOnly t => $"'{t.ToString(Migration.Args.DataParserArgs.TimeOnlyFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
#endif
            _ => value.ToString()!
        };
    }
}