// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.DbSchema;
using DbEx.Migration;
using DbEx.Migration.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.SqlServer
{
    /// <summary>
    /// Provides SQL Server specific configuration and capabilities.
    /// </summary>
    public class SqlServerSchemaConfig : DatabaseSchemaConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerSchemaConfig"/> class.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        public SqlServerSchemaConfig(string databaseName) : base(databaseName) { }

        /// <inheritdoc/>
        /// <remarks>Value is '<c>Id</c>'.</remarks>
        public override string IdColumnNameSuffix => "Id";

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
        /// <remarks>Value is '<c>Code</c>'.</remarks>
        public override string RefDataCodeColumnName => "Code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>Text</c>'.</remarks>
        public override string RefDataTextColumnName => "Text";

        /// <inheritdoc/>
        public override string ToFullyQualifiedTableName(string schema, string table) => $"[{schema}].[{table}]";

        /// <inheritdoc/>
        public override void PrepareDataParserArgs(DataParserArgs dataParserArgs)
        {
            if (dataParserArgs == null)
                return;

            if (dataParserArgs.RefDataColumnDefaults.Count == 0)
            {
                dataParserArgs.RefDataColumnDefaults.TryAdd("IsActive", _ => true);
                dataParserArgs.RefDataColumnDefaults.TryAdd("SortOrder", i => i);
            }

            dataParserArgs.CreatedByColumnName ??= CreatedByColumnName;
            dataParserArgs.CreatedDateColumnName ??= CreatedDateColumnName;
            dataParserArgs.UpdatedByColumnName ??= UpdatedByColumnName;
            dataParserArgs.UpdatedDateColumnName ??= UpdatedDateColumnName;
        }

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr) => new(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
        {
            IsNullable = dr.GetValue<string>("IS_NULLABLE").ToUpperInvariant() == "YES",
            Length = (ulong?)(dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH") <= 0 ? null : dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH")),
            Precision = (ulong?)(dr.GetValue<byte?>("NUMERIC_PRECISION") ?? dr.GetValue<short?>("DATETIME_PRECISION")),
            Scale = (ulong?)dr.GetValue<int?>("NUMERIC_SCALE"),
            DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT")
        };

        /// <inheritdoc/>
        public override async Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, DataParserArgs? dataParserArgs, CancellationToken cancellationToken)
        {
            // Configure all the single column foreign keys.
            using var sr3 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableForeignKeys.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly });
            var fks = await database.SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
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
            using var sr4 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableIdentityColumns.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly });
            await database.SqlStatement(await sr4.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
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
            using var sr5 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableAlwaysGeneratedColumns.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly });
            await database.SqlStatement(await sr5.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
            {
                var t = tables.SingleOrDefault(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                if (t == null)
                    return 0;

                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                t.Columns.Remove(c);
                return 0;
            }, cancellationToken).ConfigureAwait(false);

            // Select the generated columns.
            using var sr6 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableGeneratedColumns.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly });
            await database.SqlStatement(await sr6.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
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
            var dbType = (schema ?? throw new ArgumentNullException(nameof(schema))).Type;
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
                "NCHAR" or "CHAR" or "NVARCHAR" or "VARCHAR" or "TEXT" or "NTEXT" => "string",
                "DECIMAL" or "MONEY" or "NUMERIC" or "SMALLMONEY" => "decimal",
                "DATE" or "DATETIME" or "DATETIME2" or "SMALLDATETIME" => "DateTime",
                "ROWVERSION" or "TIMESTAMP" or "BINARY" or "VARBINARY" or "IMAGE" => "byte[]",
                "BIT" => "bool",
                "DATETIMEOFFSET" => "DateTimeOffset",
                "FLOAT" => "double",
                "INT" => "int",
                "BIGINT" => "long",
                "SMALLINT" => "short",
                "TINYINT" => "byte",
                "REAL" => "float",
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
                "TIME" => schema.Scale.HasValue && schema.Scale.Value > 0 ? $"({schema.Scale})" : string.Empty,
                "BINARY" or "VARBINARY" => $"(schema.Precision)",
                _ => string.Empty
            });

            if (includeNullability && schema.IsNullable)
                sb.Append(" NULL");

            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToFormattedSqlStatementValue(DataParserArgs dataParserArgs, object? value) => value switch
        {
            null => "NULL",
            string str => $"N'{str.Replace("'", "''", StringComparison.Ordinal)}'",
            bool b => b ? "1" : "0",
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
            "DECIMAL" or "MONEY" or "NUMERIC" or "SMALLMONEY" => true,
            _ => false
        };

        /// <inheritdoc/>
        public override bool IsDbTypeString(string? dbType) => dbType != null && dbType.ToUpperInvariant() switch
        {
            "NCHAR" or "CHAR" or "NVARCHAR" or "VARCHAR" or "TEXT" or "NTEXT" => true,
            _ => false
        };
    }
}