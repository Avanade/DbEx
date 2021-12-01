// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Schema;
using HandlebarsDotNet;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Provides the capabilities to parse database relational data.
    /// </summary>
    public class DataParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataParser"/> class.
        /// </summary>
        /// <param name="dbTables">The <see cref="DbTableSchema"/> list.</param>
        /// <param name="args">The optional <see cref="DataParserArgs"/> (will use defaults where not specified).</param>
        public DataParser(List<DbTableSchema> dbTables, DataParserArgs? args = null)
        {
            DbTables = dbTables ?? throw new ArgumentNullException(nameof(dbTables));
            Args = args ?? new DataParserArgs();
        }

        /// <summary>
        /// Gets the <see cref="DbTableSchema"/> list.
        /// </summary>
        public List<DbTableSchema> DbTables { get; }

        /// <summary>
        /// Gets the <see cref="DataParserArgs"/>.
        /// </summary>
        public DataParserArgs Args { get; }

        /// <summary>
        /// Reads and parses the database using the specified YAML <see cref="string"/>.
        /// </summary>
        /// <param name="yaml">The YAML <see cref="string"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseYamlAsync(string yaml)
        {
            using var sr = new StringReader(yaml);
            return ParseYamlAsync(sr);
        }

        /// <summary>
        /// Reads and parses the database using the specified YAML <see cref="Stream"/>.
        /// </summary>
        /// <param name="s">The YAML <see cref="Stream"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseYamlAsync(Stream s)
        {
            using var sr = new StreamReader(s);
            return ParseYamlAsync(sr);
        }

        /// <summary>
        /// Reads and parses the database using the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="tr">The YAML <see cref="TextReader"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        public Task<List<DataTable>> ParseYamlAsync(TextReader tr)
        {
            var yaml = new DeserializerBuilder().Build().Deserialize(tr)!;
            var json = new SerializerBuilder().JsonCompatible().Build().Serialize(yaml);
            return ParseJsonAsync(JObject.Parse(json));
        }

        /// <summary>
        /// Reads and parses the database using the specified <see cref="JObject"/>.
        /// </summary>
        /// <param name="json">The <see cref="JObject"/>.</param>
        /// <returns>The resulting <see cref="DataTable"/> list.</returns>
        private async Task<List<DataTable>> ParseJsonAsync(JObject json)
        {
            // Parse table/row/column data.
            var tables = new List<DataTable>();

            // Loop through all the schemas.
            foreach (var js in json.Children<JProperty>())
            {
                // Loop through the collection of tables.
                foreach (var jto in GetChildObjects(js))
                {
                    foreach (var jt in jto.Children<JProperty>())
                    {
                        var sdt = new DataTable(this, js.Name, jt.Name);
                        if (tables.Any(t => t.Schema == sdt.Schema && t.Name == sdt.Name))
                            throw new DataParserException($"Table '{sdt.Schema}.{sdt.Name}' has been specified more than once.");

                        // Loop through the collection of rows.
                        foreach (var jro in GetChildObjects(jt))
                        {
                            var row = new DataRow(sdt);

                            foreach (var jr in jro.Children<JProperty>())
                            {
                                if (jr.Value.Type == JTokenType.Object)
                                {
                                    foreach (var jc in jro.Children<JProperty>())
                                    {
                                        var col = sdt.IsRefData ? Args.RefDataCodeColumnName : sdt.DbTable.Columns.Where(x => x.IsPrimaryKey).Select(x => x.Name).SingleOrDefault();
                                        if (!string.IsNullOrEmpty(col))
                                        {
                                            row.AddColumn(col, GetColumnValue(jc.Name));
                                            foreach (var jcv in jc.Values().Where(j => j.Type == JTokenType.Property).Cast<JProperty>())
                                            {
                                                row.AddColumn(jcv.Name, GetColumnValue(jcv.Value));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (sdt.IsRefData && jro.Children().Count() == 1)
                                    {
                                        row.AddColumn(Args.RefDataCodeColumnName, GetColumnValue(jr.Name));
                                        row.AddColumn("Text", GetColumnValue(jr.Value));
                                    }
                                    else
                                        row.AddColumn(jr.Name, GetColumnValue(jr.Value));
                                }
                            }

                            sdt.AddRow(row);
                        }

                        if (sdt.Columns.Count > 0)
                        {
                            await sdt.PrepareAsync().ConfigureAwait(false);
                            tables.Add(sdt);
                        }
                    }
                }
            }

            return tables;
        }

        /// <summary>
        /// Gets the child objects.
        /// </summary>
        private static IEnumerable<JObject> GetChildObjects(JToken j)
        {
            foreach (var jc in j.Children<JArray>())
            {
                return jc.Children<JObject>();
            }

            return Array.Empty<JObject>();
        }

        /// <summary>
        /// Gets the column value.
        /// </summary>
        private object? GetColumnValue(JToken j)
        {
            return j.Type switch
            {
                JTokenType.Boolean => j.Value<bool>(),
                JTokenType.Date => j.Value<DateTime>(),
                JTokenType.Float => j.Value<float>(),
                JTokenType.Guid => j.Value<Guid>(),
                JTokenType.Integer => j.Value<int>(),
                JTokenType.TimeSpan => j.Value<TimeSpan>(),
                JTokenType.Uri => j.Value<string>(),
                JTokenType.String => GetRuntimeParameterValue(j.Value<string>()),
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
            if (value.StartsWith("^(") && value.EndsWith(")"))
            {
                var key = value[2..^1];

                // Check against known values and runtime parameters.
                switch (key)
                {
                    case "UserName": return Args.UserName;
                    case "DateTimeNow": return Args.DateTimeNow;
                    default:
                        if (Args.Parameters.TryGetValue(key, out object? dval))
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
                var mi = type.GetMethod(part[0..^2], Array.Empty<Type>());
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