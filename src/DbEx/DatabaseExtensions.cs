// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.Schema;
using OnRamp.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DbEx
{
    /// <summary>
    /// <see cref="IDatabase"/> extensions.
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Selects all the table and column schema details from the database.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="refDataPredicate">The reference data predicate used to determine whether a <see cref="DbTableSchema"/> is considered a reference data table (sets <see cref="DbTableSchema.IsRefData"/>).</param>
        /// <returns>A list of all the table and column schema details.</returns>
        /// <remarks>The <paramref name="refDataPredicate"/> where not specified will default to checking whether the <see cref="DbTableSchema"/> has any non-primary key <see cref="string"/>-based <see cref="DbTableSchema.Columns">columns</see> named '<c>Code</c>' and '<c>Text</c>'.</remarks>
        public static async Task<List<DbTableSchema>> SelectSchemaAsync(this IDatabase database, Func<DbTableSchema, bool>? refDataPredicate = null)
        {
            var tables = new List<DbTableSchema>();
            DbTableSchema? table = null;

            // Get all the tables and their columns.
            using var sr = StreamLocator.GetResourcesStreamReader("SelectTableAndColumns.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            await database.SqlStatement(await sr.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
            {
                var dt = new DbTableSchema(dr.GetValue<string>("TABLE_SCHEMA"), dr.GetValue<string>("TABLE_NAME"))
                {
                    IsAView = dr.GetValue<string>("TABLE_TYPE") == "VIEW"
                };

                if (table == null || table.Schema != dt.Schema || table.Name != dt.Name)
                    tables.Add(table = dt);

                var dc = new DbColumnSchema(table, dr.GetValue<string>("COLUMN_NAME"), dr.GetValue<string>("DATA_TYPE"))
                {
                    IsNullable = dr.GetValue<string>("IS_NULLABLE").ToUpperInvariant() == "YES",
                    Length = dr.GetValue<int?>("CHARACTER_MAXIMUM_LENGTH"),
                    Precision = dr.GetValue<byte?>("NUMERIC_PRECISION") ?? dr.GetValue<short?>("DATETIME_PRECISION"),
                    Scale = dr.GetValue<int?>("NUMERIC_SCALE"),
                    DefaultValue = dr.GetValue<string>("COLUMN_DEFAULT")
                };

                table.Columns.Add(dc);
                return 0;
            }).ConfigureAwait(false);

            // Exit where no tables initially found.
            if (tables.Count == 0)
                return tables;

            // Determine whether a table is considered reference data.
            refDataPredicate ??= new Func<DbTableSchema, bool>(t => t.Columns.Any(c => c.Name == "Code" && !c.IsPrimaryKey && c.DotNetType == "string") && t.Columns.Any(c => c.Name == "Text" && !c.IsPrimaryKey && c.DotNetType == "string"));
            foreach (var t in tables)
            {
                t.IsRefData = refDataPredicate(t);
            }

            // Configure all the single column primary and unique constraints.
            using var sr2 = StreamLocator.GetResourcesStreamReader("SelectTablePrimaryKey.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            var pks = await database.SqlStatement(await sr2.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
            {
                ConstraintName = dr.GetValue<string>("CONSTRAINT_NAME"),
                TableSchema = dr.GetValue<string>("TABLE_SCHEMA"),
                TableName = dr.GetValue<string>("TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("COLUMN_NAME"),
                IsPrimaryKey = dr.GetValue<string>("CONSTRAINT_TYPE").StartsWith("PRIMARY", StringComparison.InvariantCultureIgnoreCase)
            }).ConfigureAwait(false);

            foreach (var grp in pks.GroupBy(x => x.ConstraintName))
            {
                // Only single column unique columns are supported.
                if (grp.Count() > 1 && !grp.First().IsPrimaryKey)
                    continue;

                // Set the column flags as appropriate.
                foreach (var pk in grp)
                {
                    var col = (from t in tables
                               from c in t.Columns
                               where t.Schema == pk.TableSchema && t.Name == pk.TableName && c.Name == pk.TableColumnName
                               select c).Single();

                    if (pk.IsPrimaryKey)
                    {
                        col.IsPrimaryKey = true;
                        col.IsIdentity = col.DefaultValue != null;
                    }
                    else
                        col.IsUnique = true;
                }
            }

            // Configure all the single column foreign keys.
            using var sr3 = StreamLocator.GetResourcesStreamReader("SelectTableForeignKeys.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            var fks = await database.SqlStatement(await sr3.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
            {
                ConstraintName = dr.GetValue<string>("FK_CONSTRAINT_NAME"),
                TableSchema = dr.GetValue<string>("FK_SCHEMA_NAME"),
                TableName = dr.GetValue<string>("FK_TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("FK_COLUMN_NAME"),
                ForeignSchema = dr.GetValue<string>("UQ_SCHEMA_NAME"),
                ForeignTable = dr.GetValue<string>("UQ_TABLE_NAME"),
                ForiegnColumn = dr.GetValue<string>("UQ_COLUMN_NAME")
            }).ConfigureAwait(false);

            foreach (var grp in fks.GroupBy(x => x.ConstraintName).Where(x => x.Count() == 1))
            {
                var fk = grp.Single();
                var r = (from t in tables
                         from c in t.Columns
                         where t.Schema == fk.TableSchema && t.Name == fk.TableName && c.Name == fk.TableColumnName
                         select (t, c)).Single();

                r.c.ForeignSchema = fk.ForeignSchema;
                r.c.ForeignTable = fk.ForeignTable;
                r.c.ForeignColumn = fk.ForiegnColumn;
                r.c.IsForeignRefData = (from t in tables where t.Schema == fk.ForeignSchema && t.Name == fk.ForeignTable select t.IsRefData).FirstOrDefault();
            }

            // Select the table identity columns.
            using var sr4 = StreamLocator.GetResourcesStreamReader("SelectTableIdentityColumns.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            await database.SqlStatement(await sr4.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
            {
                var t = tables.Single(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                c.IsIdentity = true;
                c.IdentitySeed = 1;
                c.IdentityIncrement = 1;
                return 0;
            }).ConfigureAwait(false);

            // Select the "always" generated columns.
            using var sr5 = StreamLocator.GetResourcesStreamReader("SelectTableAlwaysGeneratedColumns.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            await database.SqlStatement(await sr5.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
            {
                var t = tables.Single(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                t.Columns.Remove(c);
                return 0;
            }).ConfigureAwait(false);

            // Select the generated columns.
            using var sr6 = StreamLocator.GetResourcesStreamReader("SelectTableGeneratedColumns.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly }).StreamReader!;
            await database.SqlStatement(await sr6.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
            {
                var t = tables.Single(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                c.IsComputed = true;
                return 0;
            }).ConfigureAwait(false);

            // Attempt to infer foreign key reference data relationship where not explicitly specified. 
            foreach (var t in tables)
            {
                foreach (var c in t.Columns.Where(x => !x.IsPrimaryKey && x.ForeignTable == null))
                {
                    if (!c.Name.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Find table with same name as column in any schema that is considered reference data and has a single primary key.
                    var fk = tables.Where(x => x != t && x.Name == c.Name[0..^2] && x.IsRefData && x.PrimaryKeyColumns.Count == 1).FirstOrDefault();
                    if (fk == null)
                        continue;

                    c.ForeignSchema = fk.Schema;
                    c.ForeignTable = fk.Name;
                    c.ForeignColumn = fk.PrimaryKeyColumns[0].Name;
                    c.IsForeignRefData = true;
                }
            }

            return tables;
        }
    }
}