// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Database;
using DbEx.DbSchema;
using DbEx.Migration;
using DbEx.Migration.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OnRamp.Utility;

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
        /// <param name="databaseSchemaConfig">The <see cref="DatabaseSchemaConfig"/>.</param>
        /// <param name="dataParserArgs">The optional <see cref="DataParserArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A list of all the table and column schema details.</returns>
        public static async Task<List<DbTableSchema>> SelectSchemaAsync(this IDatabase database, DatabaseSchemaConfig databaseSchemaConfig, DataParserArgs? dataParserArgs = null, CancellationToken cancellationToken = default)
        {
            var tables = new List<DbTableSchema>();
            DbTableSchema? table = null;

            dataParserArgs ??= new DataParserArgs();
            databaseSchemaConfig.PrepareDataParserArgs(dataParserArgs);
            var idColumnNameSuffix = dataParserArgs?.IdColumnNameSuffix!;
            var refDataCodeColumn = dataParserArgs?.RefDataCodeColumnName!;
            var refDataTextColumn = dataParserArgs?.RefDataTextColumnName!;
            var refDataPredicate = new Func<DbTableSchema, bool>(t => t.Columns.Any(c => c.Name == refDataCodeColumn && !c.IsPrimaryKey && c.DotNetType == "string") && t.Columns.Any(c => c.Name == refDataTextColumn && !c.IsPrimaryKey && c.DotNetType == "string"));

            // Get all the tables and their columns.
            using var sr = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTableAndColumns.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly });
#if NET7_0_OR_GREATER
            await database.SqlStatement(await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr =>
#else
            await database.SqlStatement(await sr.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr =>
#endif
            {
               if (!databaseSchemaConfig.SupportsSchema && dr.GetValue<string>("TABLE_SCHEMA") != databaseSchemaConfig.DatabaseName)
                   return 0;

                var dt = new DbTableSchema(databaseSchemaConfig, dr.GetValue<string>("TABLE_SCHEMA"), dr.GetValue<string>("TABLE_NAME"))
                {
                    IsAView = dr.GetValue<string>("TABLE_TYPE") == "VIEW"
                };

                if (table == null || table.Schema != dt.Schema || table.Name != dt.Name)
                    tables.Add(table = dt);

                var dc = databaseSchemaConfig.CreateColumnFromInformationSchema(table, dr);
                dc.IsCreatedAudit = dc.Name == dataParserArgs?.CreatedByColumnName || dc.Name == dataParserArgs?.CreatedDateColumnName;
                dc.IsUpdatedAudit = dc.Name == dataParserArgs?.UpdatedByColumnName || dc.Name == dataParserArgs?.UpdatedDateColumnName;
                dc.IsTenantId = dc.Name == dataParserArgs?.TenantIdColumnName;
                dc.IsRowVersion = dc.Name == dataParserArgs?.RowVersionColumnName;
                dc.IsIsDeleted = dc.Name == dataParserArgs?.IsDeletedColumnName;

                table.Columns.Add(dc);
                return 0;
            }, cancellationToken).ConfigureAwait(false);

            // Exit where no tables initially found.
            if (tables.Count == 0)
                return tables;

            // Determine whether a table is considered reference data.
            foreach (var t in tables)
            {
                t.IsRefData = refDataPredicate(t);
                if (t.IsRefData)
                    t.RefDataCodeColumn = t.Columns.Where(x => x.Name == refDataCodeColumn).SingleOrDefault();
            }

            // Configure all the single column primary and unique constraints.
            using var sr2 = DatabaseMigrationBase.GetRequiredResourcesStreamReader("SelectTablePrimaryKey.sql", new Assembly[] { typeof(DatabaseExtensions).Assembly });
#if NET7_0_OR_GREATER
            var pks = await database.SqlStatement(await sr2.ReadToEndAsync(cancellationToken).ConfigureAwait(false)).SelectQueryAsync(dr => new
#else
            var pks = await database.SqlStatement(await sr2.ReadToEndAsync().ConfigureAwait(false)).SelectQueryAsync(dr => new
#endif
            {
                ConstraintName = dr.GetValue<string>("CONSTRAINT_NAME"),
                TableSchema = dr.GetValue<string>("TABLE_SCHEMA"),
                TableName = dr.GetValue<string>("TABLE_NAME"),
                TableColumnName = dr.GetValue<string>("COLUMN_NAME"),
                IsPrimaryKey = dr.GetValue<string>("CONSTRAINT_TYPE").StartsWith("PRIMARY", StringComparison.InvariantCultureIgnoreCase)
            }, cancellationToken).ConfigureAwait(false);

            if (!databaseSchemaConfig.SupportsSchema)
                pks = pks.Where(x => x.TableSchema == databaseSchemaConfig.DatabaseName).ToArray();

            foreach (var grp in pks.GroupBy(x => new { x.ConstraintName, x.TableSchema, x.TableName }))
            {
                // Only single column unique columns are supported.
                if (grp.Count() > 1 && !grp.First().IsPrimaryKey)
                    continue;

                // Set the column flags as appropriate.
                foreach (var pk in grp)
                {
                    var col = (from t in tables
                               from c in t.Columns
                               where (!databaseSchemaConfig.SupportsSchema || t.Schema == pk.TableSchema) && t.Name == pk.TableName && c.Name == pk.TableColumnName
                               select c).SingleOrDefault();

                    if (col == null)
                        continue;

                    if (pk.IsPrimaryKey)
                    {
                        col.IsPrimaryKey = true;
                        if (!col.IsIdentity)
                            col.IsIdentity = col.DefaultValue != null;
                    }
                    else
                        col.IsUnique = true;
                }
            }

            // Load any additional configuration specific to the database provider.
            await databaseSchemaConfig.LoadAdditionalInformationSchema(database, tables, dataParserArgs, cancellationToken).ConfigureAwait(false);

            // Attempt to infer foreign key reference data relationship where not explicitly specified. 
            foreach (var t in tables)
            {
                foreach (var c in t.Columns.Where(x => !x.IsPrimaryKey))
                {
                    if (c.ForeignTable != null)
                    {
                        if (c.IsForeignRefData)
                            c.ForeignRefDataCodeColumn = refDataCodeColumn;

                        continue;
                    }

                    if (!c.Name.EndsWith(idColumnNameSuffix, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Find table with same name as column in any schema that is considered reference data and has a single primary key.
                    var fk = tables.Where(x => x != t && x.Name == c.Name[0..^idColumnNameSuffix.Length] && x.IsRefData && x.PrimaryKeyColumns.Count == 1).FirstOrDefault();
                    if (fk == null)
                        continue;

                    c.ForeignSchema = fk.Schema;
                    c.ForeignTable = fk.Name;
                    c.ForeignColumn = fk.PrimaryKeyColumns[0].Name;
                    c.IsForeignRefData = true;
                    c.ForeignRefDataCodeColumn = refDataCodeColumn;
                }
            }

            // Attempt to infer if a reference data column where not explicitly specified.
            var sb = new StringBuilder();

            foreach (var t in tables)
            {
                foreach (var c in t.Columns.Where(x => !x.IsPrimaryKey))
                {
                    if (c.IsForeignRefData)
                    {
                        c.IsRefData = true;
                        continue;
                    }

                    sb.Clear();
                    c.Name.Split(new char[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries).ForEach(part => sb.Append(StringConverter.ToPascalCase(part)));
                    var words = Regex.Split(sb.ToString(), DbTableSchema.WordSplitPattern).Where(x => !string.IsNullOrEmpty(x));
                    if (words.Count() > 1 && new string[] { "Id", "Code" }.Contains(words.Last(), StringComparer.InvariantCultureIgnoreCase))
                    {
                        var name = string.Join(string.Empty, words.Take(words.Count() - 1));
                        if (tables.Any(x => x.Name == name && x.Schema == t.Schema && x.IsRefData))
                            c.IsRefData = true;
                    }
                }
            }

            return tables;
        }
    }
}