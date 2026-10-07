namespace DbEx.MySql;

/// <summary>
/// Provides MySQL specific configuration and capabilities.
/// </summary>
/// <param name="migration">The owning <see cref="MySqlMigration"/>.</param>
public class MySqlSchemaConfig(MySqlMigration migration) : DatabaseSchemaConfig(migration, scriptSuffix: "mysql")
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
    /// <remarks>Value is '<c>created_on</c>'.</remarks>
    public override string CreatedOnColumnName => "created_on";

    /// <inheritdoc/>
    /// <remarks>Value is '<c>created_by</c>'.</remarks>
    public override string CreatedByColumnName => "created_by";

    /// <inheritdoc/>
    /// <remarks>Value is '<c>updated_on</c>'.</remarks>
    public override string UpdatedOnColumnName => "updated_on";

    /// <inheritdoc/>
    /// <remarks>Value is '<c>updated_by</c>'.</remarks>
    public override string UpdatedByColumnName => "updated_by";

    /// <inheritdoc/>
    /// <remarks>Value is '<c>tenant_id</c>'.</remarks>
    public override string TenantIdColumnName => "tenant_id";

    /// <inheritdoc/>
    /// <remarks>Value is '<c>row_version</c>'.</remarks>
    public override string RowVersionColumnName => "row_version";

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
    public override string ToFullyQualifiedTableName(string? schema, string table) => $"`{table}`";

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
        var dt = dr.GetValue<string>("DATA_TYPE")!;
        if (string.Compare(dt, "TINYINT", StringComparison.OrdinalIgnoreCase) == 0 && dr.GetValue<string>("COLUMN_TYPE")!.Equals("TINYINT(1)", StringComparison.OrdinalIgnoreCase))
            dt = "BOOL";

        var c = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME")!, dt)
        {
            IsNullable = dr.GetValue<string>("IS_NULLABLE")!.Equals("YES", StringComparison.OrdinalIgnoreCase),
            Length = (ulong?)dr.GetValue<long?>("CHARACTER_MAXIMUM_LENGTH"),
            Precision = dr.GetValue<ulong?>("NUMERIC_PRECISION") ?? dr.GetValue<uint?>("DATETIME_PRECISION"),
            Scale = dr.GetValue<ulong?>("NUMERIC_SCALE"),
            DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT"),
            IsDotNetDateOnly = RemovePrecisionFromDataType(dt).Equals("DATE", StringComparison.OrdinalIgnoreCase),
            IsDotNetTimeOnly = RemovePrecisionFromDataType(dt).Equals("TIME", StringComparison.OrdinalIgnoreCase),
            NativeSqlType = dr.GetValue<string?>("COLUMN_TYPE")
        };

        c.IsJsonContent = c.Type.Equals("JSON", StringComparison.OrdinalIgnoreCase) || (c.Name.EndsWith(JsonColumnNameSuffix, StringComparison.Ordinal) && c.DotNetType == "string");
        if (c.IsJsonContent && c.Name.EndsWith(JsonColumnNameSuffix, StringComparison.Ordinal))
            c.DotNetCleanedName = DbTableSchema.CreateDotNetName(c.Name[..^JsonColumnNameSuffix.Length]);

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

    /// <summary>
    /// Removes any precision from the data type.
    /// </summary>
    private static string RemovePrecisionFromDataType(string type) => type.Contains('(') ? type[..type.IndexOf('(')] : type;

    /// <inheritdoc/>
    public override async Task LoadAdditionalInformationSchema(IDatabase database, List<DbTableSchema> tables, CancellationToken cancellationToken)
    {
        // Configure all the single column foreign keys.
        using var sr3 = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"SelectTableForeignKeys.{ScriptSuffix}", [typeof(MySqlSchemaConfig).Assembly]);
#if NET7_0_OR_GREATER
        var fks = await database.SqlStatement(await sr3.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr => new
#else
        var fks = await database.SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
#endif
        {
            ConstraintName = dr.GetValue<string>("fk_constraint_name"),
            TableSchema = dr.GetValue<string>("CONSTRAINT_SCHEMA"),
            TableName = dr.GetValue<string>("TABLE_NAME"),
            TableColumnName = dr.GetValue<string>("FK_COLUMN_NAME"),
            ForeignTable = dr.GetValue<string>("REFERENCED_TABLE_NAME"),
            ForiegnColumn = dr.GetValue<string>("pk_column_name")
        }, cancellationToken).ConfigureAwait(false);

        foreach (var grp in fks.Where(x => x.TableSchema == Migration.DatabaseName).GroupBy(x => new { x.ConstraintName, x.TableSchema, x.TableName }).Where(x => x.Count() == 1))
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
        var dbType = RemovePrecisionFromDataType(schema.ThrowIfNull(nameof(schema)).Type);
        if (string.IsNullOrEmpty(dbType))
            return "string";

        if (Migration.Args.EmitDotNetDateOnly && schema.IsDotNetDateOnly)
            return "DateOnly";
        else if (Migration.Args.EmitDotNetTimeOnly && schema.IsDotNetTimeOnly)
            return "TimeOnly";

        // Source of truth: https://mysqlconnector.net/overview/mysql-to-dotnet-types/ and the Pomelo.EntityFrameworkCore.MySql type mappings (spatial types use NetTopologySuite).
        return dbType.ToUpperInvariant() switch
        {
            "CHAR" or "VARCHAR" or "TINYTEXT" or "TEXT" or "MEDIUMTEXT" or "LONGTEXT" or "SET" or "ENUM" or "NCHAR" or "NVARCHAR" or "JSON" => "string",
            "DECIMAL" or "NUMERIC" => "decimal",
            "DATETIME" or "TIMESTAMP" => "DateTime",
            "DATE" => "DateOnly",
            "TIME" => "TimeOnly",
            "BINARY" or "VARBINARY" or "TINYBLOB" or "BLOB" or "MEDIUMBLOB" or "LONGBLOB" or "VECTOR" => "byte[]",
            "BOOL" or "BOOLEAN" => "bool",
            "BIT" => schema.Precision is null or 1 ? "bool" : "ulong",
            "DOUBLE" => "double",
            "INT" or "INTEGER" or "MEDIUMINT" => "int",
            "BIGINT" => "long",
            "SMALLINT" or "YEAR" => "short",
            "TINYINT" => "byte",
            "FLOAT" => "float",
            "GEOMETRY" => "Geometry",
            "POINT" => "Point",
            "LINESTRING" => "LineString",
            "POLYGON" => "Polygon",
            "MULTIPOINT" => "MultiPoint",
            "MULTILINESTRING" => "MultiLineString",
            "MULTIPOLYGON" => "MultiPolygon",
            "GEOMETRYCOLLECTION" or "GEOMCOLLECTION" => "GeometryCollection",
            _ => throw new InvalidOperationException($"Database data type '{dbType}' does not have corresponding .NET type mapping defined."),
        };
    }

    /// <inheritdoc/>
    /// <remarks>A <c>VECTOR</c> is a <c>byte[]</c> in .NET, but is parsed from (and written as) its text (string) representation (e.g. <c>[1,2,3]</c>); likewise a multi-bit <c>BIT</c> is a <c>ulong</c> in .NET (as per the MySql.Data and Pomelo EF providers), but is parsed from its bit-string (e.g. <c>10101010</c>).</remarks>
    public override string ToDataParserTypeName(DbColumnSchema schema)
    {
        var type = schema.ThrowIfNull(nameof(schema)).Type.ToUpperInvariant();
        return (type is "VECTOR" && schema.DotNetType == "byte[]") || (type is "BIT" && schema.DotNetType == "ulong") ? "string" : base.ToDataParserTypeName(schema);
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
            "BIT" => schema.Precision.HasValue ? $"({schema.Precision.Value})" : string.Empty,
            "TIME" or "DATETIME" or "TIMESTAMP" => schema.Scale.HasValue && schema.Scale.Value > 0 ? $"({schema.Scale})" : string.Empty,
            "VECTOR" => ExtractTypeArguments(schema.NativeSqlType),
            _ => string.Empty
        });

        if (includeNullability && schema.IsNullable)
            sb.Append(" NULL");

        return sb.ToString();
    }

    /// <summary>
    /// Extracts the parenthesized type arguments (e.g. <c>(3)</c>) from a native SQL type (e.g. <c>vector(3)</c>).
    /// </summary>
    private static string ExtractTypeArguments(string? nativeSqlType)
    {
        var start = nativeSqlType?.IndexOf('(') ?? -1;
        var end = nativeSqlType?.IndexOf(')') ?? -1;
        return nativeSqlType is not null && start >= 0 && end > start ? nativeSqlType[start..(end + 1)] : string.Empty;
    }

    /// <inheritdoc/>
    public override string ToFormattedSqlStatementValue(DbColumnSchema dbColumnSchema, object? value)
    {
        if (value is string text)
        {
            var type = dbColumnSchema.Type.ToUpperInvariant();
            if (type is "VECTOR")
                return $"STRING_TO_VECTOR('{text.Replace("'", "''", StringComparison.Ordinal)}')";

            if (type is "BIT")
                return $"b'{text.Replace("'", "''", StringComparison.Ordinal)}'";

            if (type is "GEOMETRY" or "POINT" or "LINESTRING" or "POLYGON" or "MULTIPOINT" or "MULTILINESTRING" or "MULTIPOLYGON" or "GEOMETRYCOLLECTION" or "GEOMCOLLECTION")
            {
                // Supports the extended well-known text (EWKT) form of 'SRID=n;WKT' to specify the spatial reference system; the long-lat axis order is explicit as MySQL otherwise defaults to lat-long for geographic SRIDs (differs to PostGIS and SQL Server).
                var srid = string.Empty;
                if (text.StartsWith("SRID=", StringComparison.OrdinalIgnoreCase) && text.IndexOf(';') is var i and > 0 && uint.TryParse(text[5..i], out var s))
                {
                    srid = $", {s}, 'axis-order=long-lat'";
                    text = text[(i + 1)..];
                }

                return $"ST_GeomFromText('{text.Replace("'", "''", StringComparison.Ordinal)}'{srid})";
            }
        }

        return value switch
        {
            null => "NULL",
            string str => $"'{str.Replace("'", "''", StringComparison.Ordinal)}'",
            bool b when dbColumnSchema.Type.Equals("BIT", StringComparison.OrdinalIgnoreCase) => b ? "b'1'" : "b'0'",
            bool b => b ? "true" : "false",
            Guid => $"'{value}'",
            DateTime dt => $"'{dt.ToString(Migration.Args.DataParserArgs.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
            DateTimeOffset dto => $"'{dto.ToString(Migration.Args.DataParserArgs.DateTimeOffsetFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
#if NET7_0_OR_GREATER
            DateOnly d => $"'{d.ToString(Migration.Args.DataParserArgs.DateOnlyFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
            TimeOnly t => $"'{t.ToString(Migration.Args.DataParserArgs.TimeOnlyFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
#endif
            _ => value.ToString()!
        };
    }
}