// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using DbEx.DbSchema;
using HandlebarsDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides the capabilities to parse database relational data.
    /// </summary>
    public class DataParser
    {
        private class YamlNodeTypeResolver : INodeTypeResolver
        {
            private static readonly string[] boolValues = ["true", "false"];

            /// <inheritdoc/>
            bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
            {
                if (nodeEvent is Scalar scalar && scalar.Style == YamlDotNet.Core.ScalarStyle.Plain)
                {
                    if (decimal.TryParse(scalar.Value, out _))
                    {
                        if (scalar.Value.Length > 1 && scalar.Value.StartsWith('0')) // Valid JSON does not support a number that starts with a zero.
                            currentType = typeof(string);
                        else
                            currentType = typeof(decimal);

                        return true;
                    }

                    if (boolValues.Contains(scalar.Value))
                    {
                        currentType = typeof(bool);
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataParser"/> class.
        /// </summary>
        /// <param name="migration">The owning <see cref="DatabaseMigrationBase"/>.</param>
        /// <param name="dbTables">The <see cref="DbTableSchema"/> list.</param>
        internal DataParser(DatabaseMigrationBase migration, List<DbTableSchema> dbTables)
        {
            Migration = migration.ThrowIfNull(nameof(migration));
            DbTables = dbTables.ThrowIfNull(nameof(dbTables));
        }

        /// <summary>
        /// Gets the owning <see cref="DatabaseMigrationBase"/>.
        /// </summary>
        public DatabaseMigrationBase Migration { get; }

        /// <summary>
        /// Gets the <see cref="DbTableSchema"/> list.
        /// </summary>
        public IEnumerable<DbTableSchema> DbTables { get; private set; }

        /// <summary>
        /// Gets the <see cref="DataParserArgs"/>.
        /// </summary>
        public DataParserArgs ParserArgs => Migration.Args.DataParserArgs;

        /// <summary>
        /// Reads and parses the database using the specified YAML <see cref="string"/>.
        /// </summary>
        /// <param name="yaml">The YAML <see cref="string"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseYamlAsync(string yaml, CancellationToken cancellationToken = default)
        {
            using var sr = new StringReader(yaml);
            return ParseYamlAsync(sr, cancellationToken);
        }

        /// <summary>
        /// Reads and parses the database using the specified YAML <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The YAML <see cref="Stream"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseYamlAsync(Stream s, CancellationToken cancellationToken = default)
        {
            using var sr = new StreamReader(s);
            return ParseYamlAsync(sr, cancellationToken);
        }

        /// <summary>
        /// Reads and parses the database using the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="tr">The YAML <see cref="TextReader"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseYamlAsync(TextReader tr, CancellationToken cancellationToken = default)
        {
            var yaml = new DeserializerBuilder().WithNodeTypeResolver(new YamlNodeTypeResolver()).Build().Deserialize(tr)!;
            var json = new SerializerBuilder().JsonCompatible().Build().Serialize(yaml);
            return ParseJsonAsync(json, cancellationToken);
        }

        /// <summary>
        /// Reads and parses the database using the specified JSON <see cref="string"/>.
        /// </summary>
        /// <param name="json">The JSON <see cref="string"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseJsonAsync(string json, CancellationToken cancellationToken = default)
        {
            using var sr = new StringReader(json);
            return ParseJsonAsync(sr, cancellationToken);
        }

        /// <summary>
        /// Reads and parses the database using the specified JSON <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The JSON <see cref="Stream"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseJsonAsync(Stream s, CancellationToken cancellationToken = default)
        {
            using var jd = JsonDocument.Parse(s);
            return ParseJsonAsync(jd, cancellationToken);
        }

        /// <summary>
        /// Reads and parses the database using the specified JSON <see cref="TextReader"/>.
        /// </summary>
        /// <param name="tr">The JSON <see cref="TextReader"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseJsonAsync(TextReader tr, CancellationToken cancellationToken = default)
        {
            using var jd = JsonDocument.Parse(tr.ReadToEnd());
            return ParseJsonAsync(jd, cancellationToken);
        }

        /// <summary>
        /// Reads and parses the database using the specified <see cref="JsonDocument"/>.
        /// </summary>
        private async Task<List<DataTable>> ParseJsonAsync(JsonDocument json, CancellationToken cancellationToken)
        {
            // Further update/manipulate the schema.
            if (ParserArgs.DbSchemaUpdaterAsync != null)
                DbTables = await ParserArgs.DbSchemaUpdaterAsync(DbTables, cancellationToken).ConfigureAwait(false);

            // Parse table/row/column data.
            var tables = new List<DataTable>();
            DataConfig? dataConfig = null;

            // Loop through all the schemas.
            foreach (var js in json.RootElement.EnumerateObject())
            {
                // Check for data configuration as identified by the * schema - which is a special key notation.
                if (js.Name == "*")
                {
                    if (js.Value.ValueKind != JsonValueKind.Object)
                        throw new DataParserException("Data configuration ('*' schema) is invalid; must be an object.");

                    dataConfig = js.Value.Deserialize<DataConfig>();
                    continue;
                }

                // Loop through the collection of tables.
                foreach (var jto in js.Value.EnumerateArray())
                {
                    foreach (var jt in jto.EnumerateObject())
                    {
                        await ParseTableJsonAsync(tables, null, js.Name, jt, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // Applies the data configuration where specified.
            if (dataConfig is not null)
            {
                foreach (var dt in tables)
                {
                    dt.ApplyConfig(dataConfig);
                }
            }

            return tables;
        }

        /// <summary>
        /// Reads and parses the table data.
        /// </summary>
        private async Task ParseTableJsonAsync(List<DataTable> tables, DataRow? parent, string schema, JsonProperty jp, CancellationToken cancellationToken)
        {
            // Get existing or create new table.
            var sdt = new DataTable(this, Migration.SchemaConfig.SupportsSchema ? schema : string.Empty, jp.Name);
            var prev = tables.SingleOrDefault(x => x.Schema == sdt.Schema && x.Name == sdt.Name);
            if (prev is null)
                tables.Add(sdt);
            else
                sdt = prev;

            // Loop through the collection of rows.
            foreach (var jro in jp.Value.EnumerateArray())
            {
                var row = new DataRow(sdt);

                foreach (var jr in jro.EnumerateObject())
                {
                    switch (jr.Value.ValueKind)
                    {
                        case JsonValueKind.Object:
                            throw new DataParserException($"Table '{sdt.Schema}.{sdt.Name}' has unsupported '{jr.Name}' column value; must not be an object: {jr.Value}.");

                        case JsonValueKind.Array:
                            // Try parsing as a further described nested table configuration; i.e. representing a relationship.
                            await ParseTableJsonAsync(tables, row, sdt.Schema, jr, cancellationToken).ConfigureAwait(false);
                            break;

                        default:
                            if (sdt.IsRefData && jro.EnumerateObject().Count() == 1)
                            {
                                row.AddColumn(Migration.Args.RefDataCodeColumnName!, jr.Name);
                                row.AddColumn(Migration.Args.RefDataTextColumnName!, jr.Value.GetString());
                            }
                            else
                                row.AddColumn(jr.Name, GetColumnValue(jr.Value));

                            break;
                    }
                }

                // Where specified within a hierarchy attempt to be fancy and auto-update from the parent's primary key where same name.
                if (parent is not null)
                {
                    foreach (var pktc in parent.Table.DbTable.PrimaryKeyColumns)
                    {
                        var pkc = parent.Columns.SingleOrDefault(x => x.Name == pktc.Name);
                        if (pkc is not null && row.Table.DbTable.Columns.Any(x => x.Name == pktc.Name) && row.Columns.SingleOrDefault(x => x.Name == pktc.Name) is null)
                            row.AddColumn(pkc.Name, pkc.Value);
                    }
                }

                sdt.AddRow(row);
            }

            if (sdt.Columns.Count > 0)
                await sdt.PrepareAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the column value.
        /// </summary>
        private object? GetColumnValue(JsonElement j)
        {
            // TODO: Can we be smarter about the datetime parsing?!?
            return j.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => j.GetDecimal(),
                JsonValueKind.String => GetRuntimeParameterValue(j.GetString()),
                _ => null
            };
        }

        /// <summary>
        /// Get the runtime parameter value.
        /// </summary>
        private object? GetRuntimeParameterValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Get runtime value when formatted like: ^(DateTime.UtcNow)
            if (value.StartsWith("^(") && value.EndsWith(')'))
            {
                var key = value[2..^1];

                // Check against known values and runtime parameters.
                switch (key)
                {
                    case "UserName": return ParserArgs.UserName;
                    case "DateTimeNow": return ParserArgs.DateTimeNow;
                    default:
                        if (ParserArgs.Parameters.TryGetValue(key, out object? dval))
                            return dval;

                        break;
                }

                // Try instantiating as defined.
                var (val, msg) = GetSystemRuntimeValue(key);
                if (msg == null)
                    return val;

                // Try again adding the System namespace.
                (val, msg) = GetSystemRuntimeValue("System." + key);
                if (msg == null)
                    return val;

                throw new DataParserException(msg);
            }
            else
                return value;
        }

        /// <summary>
        /// Get the system runtime value.
        /// </summary>
        private static (object? value, string? message) GetSystemRuntimeValue(string param)
        {
            var ns = param.Split(",");
            if (ns.Length > 2)
                return (null, $"Runtime value parameter '{param}' is invalid; incorrect format.");

            var parts = ns[0].Split(".");
            if (parts.Length <= 1)
                return (null, $"Runtime value parameter '{param}' is invalid; incorrect format.");

            Type? type = null;
            int i = parts.Length;
            for (; i >= 0; i--)
            {
                if (ns.Length == 1)
                    type = Type.GetType(string.Join('.', parts[0..^(parts.Length - i)]));
                else
                    type = Type.GetType(string.Join('.', parts[0..^(parts.Length - i)]) + "," + ns[1]);

                if (type != null)
                    break;
            }

            if (type == null)
                return (null, $"Runtime value parameter '{param}' is invalid; no Type can be found.");

            return GetSystemPropertyValue(param, type, null, parts[i..]);
        }

        /// <summary>
        /// Recursively navigates the properties and values to discern the value.
        /// </summary>
        private static (object? value, string? message) GetSystemPropertyValue(string param, Type type, object? obj, string[] parts)
        {
            if (parts == null || parts.Length == 0)
                return (obj, null);

            var part = parts[0];
            if (part.EndsWith("()"))
            {
                var mi = type.GetMethod(part[0..^2], []);
                if (mi == null || mi.GetParameters().Length != 0)
                    return (null, $"Runtime value parameter '{param}' is invalid; specified method '{part}' is invalid.");

                return GetSystemPropertyValue(param, mi.ReturnType, mi.Invoke(obj, null), parts[1..]);
            }
            else
            {
                var pi = type.GetProperty(part);
                if (pi == null || !pi.CanRead)
                    return (null, $"Runtime value parameter '{param}' is invalid; specified property '{part}' is invalid.");

                return GetSystemPropertyValue(param, pi.PropertyType, pi.GetValue(obj, null), parts[1..]);
            }
        }
    }
}