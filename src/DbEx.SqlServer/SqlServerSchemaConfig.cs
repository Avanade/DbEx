// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.DbSchema;
using OnRamp.Utility;
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
    public class SqlServerSchemaConfig : DbDatabaseSchemaConfig
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
        public override string GetFullyQualifiedTableName(string schema, string table) => $"[{schema}].[{table}]";

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr) => new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
        {
            IsNullable = dr.GetValue<string>("IS_NULLABLE").ToUpperInvariant() == "YES",
            Length = (ulong?)dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH"),
            Precision = (ulong?)(dr.GetValue<byte?>("NUMERIC_PRECISION") ?? dr.GetValue<short?>("DATETIME_PRECISION")),
            Scale = (ulong?)dr.GetValue<int?>("NUMERIC_SCALE"),
            DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT")
        };

        /// <inheritdoc/>
        public override async Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, CancellationToken cancellationToken)
        {
            // Select the table identity columns.
            using var sr4 = StreamLocator.GetResourcesStreamReader("SelectTableIdentityColumns.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly }).StreamReader!;
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
            using var sr5 = StreamLocator.GetResourcesStreamReader("SelectTableAlwaysGeneratedColumns.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly }).StreamReader!;
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
            using var sr6 = StreamLocator.GetResourcesStreamReader("SelectTableGeneratedColumns.sql", new Assembly[] { typeof(SqlServerSchemaConfig).Assembly }).StreamReader!;
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
        public override string GetDotNetTypeName(string? dbType)
        {
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
        public override string GetFormattedSqlType(DbColumnSchema schema)
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

            if (schema.IsNullable)
                sb.Append(" NULL");

            return sb.ToString();
        }
    }
}