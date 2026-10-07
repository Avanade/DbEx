namespace DbEx.Postgres;

/// <summary>
/// Provides PostgreSQL specific configuration and capabilities.
/// </summary>
/// <param name="migration">The owning <see cref="PostgresMigration"/>.</param>
public class PostgresSchemaConfig(PostgresMigration migration) : DatabaseSchemaConfig(migration, true, "public", "pgsql")
{
    private readonly HashSet<string> _enumTypeNames = new(StringComparer.Ordinal);

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
        var c = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME")!, dr.GetValue<string>("DATA_TYPE")!)
        {
            IsNullable = dr.GetValue<string>("IS_NULLABLE")!.Equals("YES", StringComparison.OrdinalIgnoreCase),
            Length = (ulong?)dr.GetValue<long?>("CHARACTER_MAXIMUM_LENGTH"),
            Precision = (ulong?)(dr.GetValue<int?>("NUMERIC_PRECISION") ?? dr.GetValue<int?>("DATETIME_PRECISION")),
            Scale = (ulong?)dr.GetValue<int?>("NUMERIC_SCALE"),
            DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT") is not null && dr.GetValue<string>("COLUMN_DEFAULT")!.StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ? null : dr.GetValue<string>("COLUMN_DEFAULT"),
            IsComputed = dr.GetValue<string?>("IS_GENERATED") != "NEVER",
            IsIdentity = dr.GetValue<string>("COLUMN_DEFAULT")?.StartsWith("nextval(", StringComparison.OrdinalIgnoreCase) ?? false,
            IsDotNetDateOnly = RemovePrecisionFromDataType(dr.GetValue<string>("DATA_TYPE")!).Equals("DATE", StringComparison.OrdinalIgnoreCase),
            IsDotNetTimeOnly = RemovePrecisionFromDataType(dr.GetValue<string>("DATA_TYPE")!).Equals("TIME WITHOUT TIME ZONE", StringComparison.OrdinalIgnoreCase),
            UdtName = dr.GetValue<string?>("udt_name")
        };

        c.IsJsonContent = c.Type.Equals("JSON", StringComparison.OrdinalIgnoreCase) || (c.Name.EndsWith(JsonColumnNameSuffix, StringComparison.Ordinal) && c.DotNetType == "string");
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

        // Select the full native types (including type modifiers) and enum types; these are not available via INFORMATION_SCHEMA for the likes of array, enum and extension types (e.g. PostGIS, pgvector).
        _enumTypeNames.Clear();
        using var sr2 = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"SelectTableNativeTypes.{ScriptSuffix}", [typeof(PostgresSchemaConfig).Assembly]);
        await database.SqlStatement(await sr2.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr =>
        {
            if (dr.GetValue<string>("type_kind") == "e")
                _enumTypeNames.Add(dr.GetValue<string>("udt_name")!);

            if (dr.GetValue<string?>("element_type_kind") == "e")
                _enumTypeNames.Add(dr.GetValue<string>("element_udt_name")!);

            var t = tables.SingleOrDefault(x => x.Schema == dr.GetValue<string>("table_schema") && x.Name == dr.GetValue<string>("table_name"));
            var c = t?.Columns.SingleOrDefault(x => x.Name == dr.GetValue<string>("column_name"));
            if (c is not null)
                c.NativeSqlType = dr.GetValue<string>("native_type");

            return 0;
        }, cancellationToken).ConfigureAwait(false);

        // Configure all the single column foreign keys.
        using var sr3 = DatabaseMigrationBase.GetRequiredResourcesStreamReader($"SelectTableForeignKeys.{ScriptSuffix}", [typeof(PostgresSchemaConfig).Assembly]);
        var fks = await database.SqlStatement(await sr3.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr => new
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
            "TIMESTAMP WITHOUT TIME ZONE" => "DateTime",
            "TIME WITH TIME ZONE" or "TIMESTAMP WITH TIME ZONE" => "DateTimeOffset",
            "INTERVAL" => "TimeSpan",
            "TIME WITHOUT TIME ZONE" => "TimeOnly",
            "DATE" => "DateOnly",
            "BYTEA" => "byte[]",
            "BOOLEAN" => "bool",
            "DOUBLE PRECISION" => "double",
            "INTEGER" => "int",
            "BIGINT" => "long",
            "SMALLINT" => "short",
            "REAL" => "float",
            "UUID" => "Guid",
            "XID" => "uint",
            "BIT" => schema.Length == 1 ? "bool" : "BitArray",
            "ARRAY" => ToDotNetArrayElementTypeName(schema),
            _ => ToDotNetTypeNameFromUdtName(schema.UdtName, schema) ?? throw new InvalidOperationException($"Database data type '{dbType}'{(string.IsNullOrEmpty(schema.UdtName) ? string.Empty : $" ('{schema.UdtName}')")} does not have corresponding .NET type mapping defined."),
        };
    }

    /// <summary>
    /// Gets the .NET array type name for an <c>ARRAY</c> where the element type is derived from the <see cref="DbColumnSchema.UdtName"/> (internal names are prefixed with an underscore).
    /// </summary>
    private string ToDotNetArrayElementTypeName(DbColumnSchema schema)
    {
        var udt = schema.UdtName?.TrimStart('_');
        var element = string.IsNullOrEmpty(udt) ? null : ToDotNetTypeNameFromUdtName(udt, null);
        return element is null
            ? throw new InvalidOperationException($"Database data type 'ARRAY' ('{schema.UdtName}') does not have corresponding .NET type mapping defined.")
            : $"{element}[]";
    }

    /// <summary>
    /// Gets the .NET type name for the PostgreSQL internal (<c>udt_name</c>) type name; <c>null</c> where not mapped.
    /// </summary>
    /// <remarks>This covers the built-in types not already mapped by <see cref="ToDotNetTypeName(DbColumnSchema)"/>, enums, and the more well-known extensions (PostGIS, pgvector, hstore, ltree and citext) using the Npgsql and EF Core provider conventions.</remarks>
    private string? ToDotNetTypeNameFromUdtName(string? udtName, DbColumnSchema? schema)
    {
        if (string.IsNullOrEmpty(udtName))
            return null;

        if (_enumTypeNames.Contains(udtName))
            return "string";

        return udtName.ToLowerInvariant() switch
        {
            "text" or "varchar" or "bpchar" or "citext" or "json" or "jsonb" or "jsonpath" or "xml" or "name" or "ltree" or "lquery" or "ltxtquery" => "string",
            "numeric" or "money" => "decimal",
            "timestamp" => "DateTime",
            "timetz" or "timestamptz" => "DateTimeOffset",
            "interval" => "TimeSpan",
            "time" => "TimeOnly",
            "date" => "DateOnly",
            "bytea" => "byte[]",
            "bool" => "bool",
            "float8" => "double",
            "float4" => "float",
            "int8" => "long",
            "int4" => "int",
            "int2" => "short",
            "uuid" => "Guid",
            "oid" or "xid" or "cid" => "uint",
            "xid8" => "ulong",
            "char" => "char",
            "pg_lsn" => "NpgsqlLogSequenceNumber",
            "bit" => schema?.Length == 1 ? "bool" : "BitArray",
            "varbit" => "BitArray",
            "inet" => "IPAddress",
            "cidr" => "NpgsqlCidr",
            "macaddr" or "macaddr8" => "PhysicalAddress",
            "tsvector" => "NpgsqlTsVector",
            "tsquery" => "NpgsqlTsQuery",
            "point" => "NpgsqlPoint",
            "line" => "NpgsqlLine",
            "lseg" => "NpgsqlLSeg",
            "box" => "NpgsqlBox",
            "path" => "NpgsqlPath",
            "polygon" => "NpgsqlPolygon",
            "circle" => "NpgsqlCircle",
            "hstore" => "Dictionary<string, string?>",
            "geometry" or "geography" => ToDotNetSpatialTypeName(schema?.NativeSqlType),
            "vector" => "Vector",
            "halfvec" => "HalfVector",
            "sparsevec" => "SparseVector",
            "int4range" => "NpgsqlRange<int>",
            "int8range" => "NpgsqlRange<long>",
            "numrange" => "NpgsqlRange<decimal>",
            "daterange" => "NpgsqlRange<DateOnly>",
            "tsrange" or "tstzrange" => "NpgsqlRange<DateTime>",
            "int4multirange" => "NpgsqlRange<int>[]",
            "int8multirange" => "NpgsqlRange<long>[]",
            "nummultirange" => "NpgsqlRange<decimal>[]",
            "datemultirange" => "NpgsqlRange<DateOnly>[]",
            "tsmultirange" or "tstzmultirange" => "NpgsqlRange<DateTime>[]",
            _ => null
        };
    }

    /// <summary>
    /// Gets the NetTopologySuite type name from the PostGIS native type (e.g. <c>geometry(Point,4326)</c>); defaults to <c>Geometry</c> where not specific.
    /// </summary>
    private static string ToDotNetSpatialTypeName(string? nativeSqlType)
    {
        var start = nativeSqlType?.IndexOf('(') ?? -1;
        if (nativeSqlType is null || start < 0)
            return "Geometry";

        var subtype = nativeSqlType[(start + 1)..].Split(',', ')')[0].Trim();
        if (subtype.EndsWith("ZM", StringComparison.OrdinalIgnoreCase))
            subtype = subtype[..^2];
        else if (subtype.EndsWith('Z') || subtype.EndsWith('z') || subtype.EndsWith('M') || subtype.EndsWith('m'))
            subtype = subtype[..^1];

        return subtype.ToUpperInvariant() switch
        {
            "POINT" => "Point",
            "LINESTRING" => "LineString",
            "POLYGON" => "Polygon",
            "MULTIPOINT" => "MultiPoint",
            "MULTILINESTRING" => "MultiLineString",
            "MULTIPOLYGON" => "MultiPolygon",
            "GEOMETRYCOLLECTION" => "GeometryCollection",
            _ => "Geometry"
        };
    }

    private static readonly HashSet<string> _structTypeNames = new(StringComparer.Ordinal)
    {
        "NpgsqlLogSequenceNumber", "NpgsqlCidr", "NpgsqlPoint", "NpgsqlLine", "NpgsqlLSeg", "NpgsqlBox", "NpgsqlPath", "NpgsqlPolygon", "NpgsqlCircle"
    };

    /// <inheritdoc/>
    public override bool IsDotNetTypeAClass(DbColumnSchema schema)
    {
        var type = schema.ThrowIfNull(nameof(schema)).DotNetType;
        if (_structTypeNames.Contains(type) || (type.StartsWith("NpgsqlRange<", StringComparison.Ordinal) && !type.EndsWith("[]", StringComparison.Ordinal)))
            return false;

        return base.IsDotNetTypeAClass(schema);
    }

    /// <inheritdoc/>
    public override string ToFormattedSqlType(DbColumnSchema schema, bool includeNullability = true)
    {
        var sb = new StringBuilder();
        if (schema.Type.Equals("ARRAY", StringComparison.OrdinalIgnoreCase) || schema.Type.Equals("USER-DEFINED", StringComparison.OrdinalIgnoreCase))
        {
            // Use the native type as-is (it is already correctly quoted/qualified where required); upper-casing is not safe.
            sb.Append(schema.NativeSqlType ?? (schema.Type.Equals("ARRAY", StringComparison.OrdinalIgnoreCase) ? $"{schema.UdtName?.TrimStart('_')}[]" : schema.UdtName));
        }
        else
        {
            sb.Append(schema.Type!.ToUpperInvariant());
            sb.Append(schema.Type.ToUpperInvariant() switch
            {
                "CHARACTER VARYING" or "CHARACTER" => schema.Length.HasValue && schema.Length.Value > 0 ? $"({schema.Length.Value})" : "(MAX)",
                "BIT" or "BIT VARYING" => schema.Length.HasValue && schema.Length.Value > 0 ? $"({schema.Length.Value})" : string.Empty,
                "NUMERIC" => $"({schema.Precision}, {schema.Scale})",
                "TIMESTAMP WITHOUT TIME ZONE" or "TIMESTAMP WITH TIME ZONE" or "TIME WITH TIME ZONE" or "TIME WITHOUT TIME ZONE" => schema.Scale.HasValue && schema.Scale.Value > 0 ? $"({schema.Scale})" : string.Empty,
                _ => string.Empty
            });
        }

        if (includeNullability && schema.IsNullable)
            sb.Append(" NULL");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public override string ToFormattedSqlStatementValue(DbColumnSchema dbColumnSchema, object? value) => value switch
    {
        null => "NULL",
        string str => $"'{str.Replace("'", "''", StringComparison.Ordinal)}'",
        bool b when dbColumnSchema.Type.StartsWith("BIT", StringComparison.OrdinalIgnoreCase) => b ? "B'1'" : "B'0'",
        bool b => b ? "true" : "false",
        Guid => $"uuid('{value}')",
        DateTime dt => $"'{dt.ToString(Migration.Args.DataParserArgs.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
        DateTimeOffset dto => $"'{dto.ToString(Migration.Args.DataParserArgs.DateTimeOffsetFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
#if NET7_0_OR_GREATER
        DateOnly d => $"'{d.ToString(Migration.Args.DataParserArgs.DateOnlyFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
        TimeOnly t => $"'{t.ToString(Migration.Args.DataParserArgs.TimeOnlyFormat, System.Globalization.CultureInfo.InvariantCulture)}'",
#endif
        _ => value.ToString()!
    };
}