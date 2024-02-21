﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

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

namespace DbEx.MySql
{
    /// <summary>
    /// Provides MySQL specific configuration and capabilities.
    /// </summary>
    /// <param name="databaseName">The database name.</param>
    public class MySqlSchemaConfig(string databaseName) : DatabaseSchemaConfig(databaseName)
    {
        /// <inheritdoc/>
        /// <remarks>Value is '<c>_id</c>'.</remarks>
        public override string IdColumnNameSuffix { get; set; } = "_id";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>created_date</c>'.</remarks>
        public override string CreatedDateColumnName { get; set; } = "created_date";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>created_by</c>'.</remarks>
        public override string CreatedByColumnName { get; set; } = "created_by";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>updated_date</c>'.</remarks>
        public override string UpdatedDateColumnName { get; set; } = "updated_date";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>updated_by</c>'.</remarks>
        public override string UpdatedByColumnName { get; set; } = "updated_by";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>tenant_id</c>'.</remarks>
        public override string TenantIdColumnName { get; set; } = "tenant_id";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>row_version</c>'.</remarks>
        public override string RowVersionColumnName { get; set; } = "row_version";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>is_deleted</c>'.</remarks>
        public override string IsDeletedColumnName { get; set; } = "is_deleted";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>code</c>'.</remarks>
        public override string RefDataCodeColumnName { get; set; } = "code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>text</c>'.</remarks>
        public override string RefDataTextColumnName { get; set; } = "text";

        /// <inheritdoc/>
        public override string ToFullyQualifiedTableName(string? schema, string table) => $"`{table}`";

        /// <inheritdoc/>
        public override void PrepareDataParserArgs(DataParserArgs dataParserArgs)
        {
            base.PrepareDataParserArgs(dataParserArgs);

            if (dataParserArgs.RefDataColumnDefaults.Count == 0)
            {
                dataParserArgs.RefDataColumnDefaults.TryAdd("is_active", _ => true);
                dataParserArgs.RefDataColumnDefaults.TryAdd("sort_order", i => i);
            }
        }

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr)
        {
            var dt = dr.GetValue<string>("DATA_TYPE");
            if (string.Compare(dt, "TINYINT", StringComparison.InvariantCultureIgnoreCase) == 0 && dr.GetValue<string>("COLUMN_TYPE").ToUpperInvariant() == "TINYINT(1)")
                dt = "BOOL";

            var c = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME"), dt)
            {
                IsNullable = dr.GetValue<string>("IS_NULLABLE").ToUpperInvariant() == "YES",
                Length = (ulong?)dr.GetValue<long?>("CHARACTER_MAXIMUM_LENGTH"),
                Precision = dr.GetValue<ulong?>("NUMERIC_PRECISION") ?? dr.GetValue<uint?>("DATETIME_PRECISION"),
                Scale = dr.GetValue<ulong?>("NUMERIC_SCALE"),
                DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT")
            };

            // https://dev.mysql.com/doc/refman/5.7/en/show-columns.html
            var extra = dr.GetValue<string?>("EXTRA");
            if (extra is not null)
            {
                if (extra.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase))
                {
                    c.IsIdentity = true;
                    c.IdentitySeed = 1;
                    c.IdentityIncrement = 1;
                }

                if (extra.Contains("GENERATED", StringComparison.OrdinalIgnoreCase))
                    c.IsComputed = true;
            }

            return c;
        }

        /// <inheritdoc/>
        public override async Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, DataParserArgs? dataParserArgs, CancellationToken cancellationToken)
        {
            // Configure all the single column foreign keys.
            using var sr3 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableForeignKeys.sql", [typeof(MySqlSchemaConfig).Assembly]);
            var fks = await database.SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
            {
                ConstraintName = dr.GetValue<string>("fk_constraint_name"),
                TableSchema = dr.GetValue<string>("CONSTRAINT_SCHEMA"),
                TableName = dr.GetValue<string>("TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("FK_COLUMN_NAME"),
                ForeignTable = dr.GetValue<string>("REFERENCED_TABLE_NAME"),
                ForiegnColumn = dr.GetValue<string>("pk_column_name")
            }, cancellationToken).ConfigureAwait(false);

            foreach (var grp in fks.Where(x => x.TableSchema == DatabaseName).GroupBy(x => new { x.ConstraintName, x.TableSchema, x.TableName }).Where(x => x.Count() == 1))
            {
                var fk = grp.Single();
                var r = (from t in tables
                         from c in t.Columns
                         where (t.Name == fk.TableName && c.Name == fk.TableColumnName)
                         select (t, c)).SingleOrDefault();

                if (r == default)
                    continue;

                r.c.ForeignSchema = string.Empty;
                r.c.ForeignTable = fk.ForeignTable;
                r.c.ForeignColumn = fk.ForiegnColumn;
                r.c.IsForeignRefData = (from t in tables where (t.Schema == string.Empty && t.Name == fk.ForeignTable) select t.IsRefData).FirstOrDefault();
            }
        }

        /// <inheritdoc/>
        public override string ToDotNetTypeName(DbColumnSchema schema)
        {
            var dbType = schema.ThrowIfNull(nameof(schema)).Type;
            if (string.IsNullOrEmpty(dbType))
                return "string";

            if (dbType.EndsWith(')'))
            {
                var i = dbType.LastIndexOf('(');
                if (i > 0)
                    dbType = dbType[..i];
            }

            return dbType.ToUpperInvariant() switch
            {
                "CHAR" or "VARCHAR" or "TINYTEXT" or "TEXT" or "MEDIUMTEXT" or "LONGTEXT" or "SET" or "ENUM" or "NCHAR" or "NVARCHAR" or "JSON" => "string",
                "DECIMAL" => "decimal",
                "DATE" or "DATETIME" or "TIMESTAMP" => "DateTime",
                "BINARY" or "VARBINARY" or "TINYBLOB" or "BLOB" or "MEDIUMBLOB" or "LONGBLOB" => "byte[]",
                "BIT" or "BOOL" or "BOOLEAN" => "bool",
                "DATETIMEOFFSET" => "DateTimeOffset",
                "DOUBLE" => "double",
                "INT" => "int",
                "BIGINT" => "long",
                "SMALLINT" => "short",
                "TINYINT" => "byte",
                "FLOAT" => "float",
                "TIME" => "TimeSpan",
                "UNIQUEIDENTIFIER" => "Guid",
                _ => throw new InvalidOperationException($"Database data type '{dbType}' does not have corresponding .NET type mapping defined."),
            };
        }

        /// <inheritdoc/>
        public override string ToFormattedSqlType(DbColumnSchema schema, bool includeNullability = true)
        {
            var sb = new StringBuilder(schema.Type!.ToUpperInvariant());
            
            sb.Append(schema.Type.ToUpperInvariant() switch
            {
                "CHAR" or "VARCHAR" or "NCHAR" or "NVARCHAR" => schema.Length.HasValue && schema.Length.Value > 0 ? $"({schema.Length.Value})" : "(MAX)",
                "DECIMAL" => $"({schema.Precision}, {schema.Scale})",
                "NUMERIC" => $"({schema.Precision}, {schema.Scale})",
                "TIME" or "DATETIME" or "TIMESTAMP" => schema.Scale.HasValue && schema.Scale.Value > 0 ? $"({schema.Scale})" : string.Empty,
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
            "INT" or "BIGINT" or "SMALLINT" or "TINYINT" => true,
            _ => false
        };

        /// <inheritdoc/>
        public override bool IsDbTypeDecimal(string? dbType) => dbType != null && dbType.ToUpperInvariant() switch
        {
            "DECIMAL" => true,
            _ => false
        };

        /// <inheritdoc/>
        public override bool IsDbTypeString(string? dbType) => dbType != null && dbType.ToUpperInvariant() switch
        {
            "CHAR" or "VARCHAR" or "TINYTEXT" or "TEXT" or "MEDIUMTEXT" or "LONGTEXT" or "SET" or "ENUM" or "NCHAR" or "NVARCHAR" or "JSON" => true,
            _ => false
        };
    }
}