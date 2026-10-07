using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.Test
{
    /// <summary>
    /// Reads back (as text) the extra types rows loaded by the migration to verify the data load round trip.
    /// </summary>
    internal static class ExtraTypesReader
    {
        public static async Task<List<Dictionary<string, string?>>> ReadAsync(DbConnection connection, string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            using var dr = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            var rows = new List<Dictionary<string, string?>>();
            while (await dr.ReadAsync().ConfigureAwait(false))
            {
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < dr.FieldCount; i++)
                    row[dr.GetName(i)] = dr.IsDBNull(i) ? null : Convert.ToString(dr.GetValue(i), CultureInfo.InvariantCulture);

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// Asserts the first row matches <paramref name="expected"/> exactly, and that the second row only has the identifier value (all other columns <c>null</c>).
        /// </summary>
        public static void AssertRows(List<Dictionary<string, string?>> rows, string idColumn, Dictionary<string, string?> expected)
        {
            Assert.That(rows, Has.Count.EqualTo(2));

            var errors = new List<string>();
            foreach (var kv in expected.Where(x => rows[0].GetValueOrDefault(x.Key) != x.Value || !rows[0].ContainsKey(x.Key)))
                errors.Add($"row 1 '{kv.Key}': expected '{kv.Value}' but was '{(rows[0].TryGetValue(kv.Key, out var v) ? v : "<missing column>")}'");

            foreach (var kv in rows[0].Where(x => !expected.ContainsKey(x.Key)))
                errors.Add($"row 1 '{kv.Key}': unexpected column (add to expected).");

            Assert.That(rows[1][idColumn], Is.EqualTo("2"));
            foreach (var kv in rows[1].Where(x => x.Value is not null && !string.Equals(x.Key, idColumn, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"row 2 '{kv.Key}': expected null but was '{kv.Value}'");

            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
        }
    }
}
