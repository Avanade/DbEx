// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.DbSchema;
using System;
using System.Text;

namespace DbEx.MySql
{
    /// <summary>
    /// Provides MySQL specific configuration and capabilities.
    /// </summary>
    public class MySqlSchemaConfig : DbDatabaseSchemaConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlSchemaConfig"/> class.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        public MySqlSchemaConfig(string databaseName) : base(databaseName, false) { }

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
        /// <remarks>Value is '<c>code</c>'.</remarks>
        public override string RefDataCodeColumnName => "code";

        /// <inheritdoc/>
        /// <remarks>Value is '<c>text</c>'.</remarks>
        public override string RefDataTextColumnName => "text";

        /// <inheritdoc/>
        public override string GetFullyQualifiedTableName(string schema, string table) => $"`{table}`";

        /// <inheritdoc/>
        public override DbColumnSchema CreateColumnFromInformationSchema(DbTableSchema table, DatabaseRecord dr)
        {
            var c = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
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
        public override string GetFormattedSqlType(DbColumnSchema schema)
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

            if (schema.IsNullable)
                sb.Append(" NULL");

            return sb.ToString();
        }

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