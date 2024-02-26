// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using CoreEx.Database;
using DbEx.DbSchema;
using DbEx.Migration;
using DbEx.SqlServer.Migration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.SqlServer
{
    /// <summary>
    /// Provides SQL Server specific configuration and capabilities.
    /// </summary>
    /// <param name="migration">The owning <see cref="SqlServerMigration"/>.</param>
    public class SqlServerSchemaConfig(SqlServerMigration migration) : DatabaseSchemaConfig(migration, true, "dbo")
    {
        /// <inheritdoc/>
        /// <remarks>Value is '<c>Id</c>'.</remarks>
        public override string IdColumnNameSuffix => "Id";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>Code</c>'.</remarks>
        public override string CodeColumnNameSuffix => "Code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>Json</c>'.</remarks>
        public override string JsonColumnNameSuffix => "Json";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>CreatedDate</c>'.</remarks>
        public override string CreatedDateColumnName => "CreatedDate";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>CreatedBy</c>'.</remarks>
        public override string CreatedByColumnName => "CreatedBy";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>UpdatedDate</c>'.</remarks>
        public override string UpdatedDateColumnName => "UpdatedDate";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>UpdatedBy</c>'.</remarks>
        public override string UpdatedByColumnName => "UpdatedBy";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>TenantId</c>'.</remarks>
        public override string TenantIdColumnName => "TenantId";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>RowVersion</c>'.</remarks>
        public override string RowVersionColumnName => "RowVersion";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>IsDeleted</c>'.</remarks>
        public override string IsDeletedColumnName => "IsDeleted";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>Code</c>'.</remarks>
        public override string RefDataCodeColumnName => "Code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>Text</c>'.</remarks>
        public override string RefDataTextColumnName => "Text";

        /// <inheritdoc/>
        public override string ToFullyQualifiedTableName(string? schema, string table) => $"[{schema}].[{table}]";

        /// <inheritdoc/>
        public override void PrepareMigrationArgs()
        {
            base.PrepareMigrationArgs();

            Migration.Args.DataParserArgs.RefDataColumnDefaults.TryAdd("IsActive", _ => true);
            Migration.Args.DataParserArgs.RefDataColumnDefaults.TryAdd("SortOrder", i => i);
        }

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr)
        {
            var c = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
            {
                IsNullable = dr.GetValue<string>("IS_NULLABLE").Equals("YES", StringComparison.OrdinalIgnoreCase),
                Length = (ulong?)(dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH") <= 0 ? null : dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH")),
                Precision = (ulong?)(dr.GetValue<byte?>("NUMERIC_PRECISION") ?? dr.GetValue<short?>("DATETIME_PRECISION")),
                Scale = (ulong?)dr.GetValue<int?>("NUMERIC_SCALE"),
                DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT"),
                IsDotNetDateOnly = RemovePrecisionFromDataType(dr.GetValue<string>("DATA_TYPE")).Equals("DATE", StringComparison.OrdinalIgnoreCase),
                IsDotNetTimeOnly = RemovePrecisionFromDataType(dr.GetValue<string>("DATA_TYPE")).Equals("TIME", StringComparison.OrdinalIgnoreCase),
            };

            if (c.IsJsonContent = c.DotNetName == "string" && c.Name.EndsWith(JsonColumnNameSuffix, StringComparison.Ordinal))
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
            // Configure all the single column foreign keys.
            using var sr3 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableForeignKeys.sql", [typeof(SqlServerSchemaConfig).Assembly]);
#if NET7_0_OR_GREATER
            var fks = await database.SqlStatement(await sr3.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr => new
#else
            var fks = await database.SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
#endif
            {
                ConstraintName = dr.GetValue<string>("FK_CONSTRAINT_NAME"),
                TableSchema = dr.GetValue<string>("FK_SCHEMA_NAME"),
                TableName = dr.GetValue<string>("FK_TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("FK_COLUMN_NAME"),
                ForeignSchema = dr.GetValue<string>("UQ_SCHEMA_NAME"),
                ForeignTable = dr.GetValue<string>("UQ_TABLE_NAME"),
                ForiegnColumn = dr.GetValue<string>("UQ_COLUMN_NAME")
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

            // Select the table identity columns.
            using var sr4 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableIdentityColumns.sql", [typeof(SqlServerSchemaConfig).Assembly]);
#if NET7_0_OR_GREATER
            await database.SqlStatement(await sr4.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr =>
#else
            await database.SqlStatement(await sr4.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
#endif
            {
                var t = tables.SingleOrDefault(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                if (t == null)
                    return 0;

                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                c.IsIdentity = true;
                c.IdentitySeed = 1;
                c.IdentityIncrement = 1;
                return 0;
            }, cancellationToken).ConfigureAwait(false);

            // Select the "always" generated columns.
            using var sr5 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableAlwaysGeneratedColumns.sql", [typeof(SqlServerSchemaConfig).Assembly]);
#if NET7_0_OR_GREATER
            await database.SqlStatement(await sr5.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr =>
#else
            await database.SqlStatement(await sr5.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
#endif
            {
                var t = tables.SingleOrDefault(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                if (t == null)
                    return 0;

                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                t.Columns.Remove(c);
                return 0;
            }, cancellationToken).ConfigureAwait(false);

            // Select the generated columns.
            using var sr6 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableGeneratedColumns.sql", [typeof(SqlServerSchemaConfig).Assembly]);
#if NET7_0_OR_GREATER
            await database.SqlStatement(await sr6.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr =>
#else
            await database.SqlStatement(await sr6.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
#endif
            {
                var t = tables.SingleOrDefault(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                if (t == null)
                    return 0;

                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                c.IsComputed = true;
                return 0;
            }, cancellationToken).ConfigureAwait(false);
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

            return dbType.ToUpperInvariant() switch
            {
                "NCHAR" or "CHAR" or "NVARCHAR" or "VARCHAR" or "TEXT" or "NTEXT" => "string",
                "DECIMAL" or "MONEY" or "NUMERIC" or "SMALLMONEY" => "decimal",
                "DATETIME" or "DATETIME2" or "SMALLDATETIME" => "DateTime",
                "DATETIMEOFFSET" => "DateTimeOffset",
                "DATE" => "DateTime", // Date only
                "TIME" => "TimeSpan", // Time only
                "ROWVERSION" or "TIMESTAMP" or "BINARY" or "VARBINARY" or "IMAGE" => "byte[]",
                "BIT" => "bool",
                "FLOAT" => "double",
                "INT" => "int",
                "BIGINT" => "long",
                "SMALLINT" => "short",
                "TINYINT" => "byte",
                "REAL" => "float",
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
                "TIME" => schema.Scale.HasValue && schema.Scale.Value > 0 ? $"({schema.Scale})" : string.Empty,
                "BINARY" or "VARBINARY" => $"(schema.Precision)",
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
            string str => $"N'{str.Replace("'", "''", StringComparison.Ordinal)}'",
            bool b => b ? "1" : "0",
            Guid => $"CONVERT(UNIQUEIDENTIFIER, '{value}')",
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