// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Utility;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.SqlServer
{
    /// <summary>
    /// Provides <see href="https://docs.microsoft.com/en-us/sql/connect/ado-net/microsoft-ado-net-sql-server">SQL Server</see> extension methods.
    /// </summary>
    public static class SqlServerExtensions
    {
        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="dpc">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>A <see cref="DbParameter"/>.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static SqlParameter AddTableValuedParameter(this DatabaseParameterCollection dpc, string name, TableValuedParameter tvp)
        {
            var p = (SqlParameter)(dpc ?? throw new ArgumentNullException(nameof(dpc))).Command.CreateParameter();
            p.ParameterName = name ?? throw new ArgumentNullException(nameof(name));
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = tvp.TypeName;
            p.Value = tvp.Value;
            p.Direction = ParameterDirection.Input;

            dpc.Command.Parameters.Add(p);
            return p;
        }

        /// <summary>
        /// Adds the named <see cref="TableValuedParameter"/> value to the <see cref="DbCommand.Parameters"/>.
        /// </summary>
        /// <param name="dpc">The <see cref="DatabaseParameterCollection"/>.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="tvp">The <see cref="TableValuedParameter"/> value.</param>
        /// <returns>The <see cref="DatabaseParameterCollection"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This specifically implies that the <see cref="SqlParameter"/> is being used; if not then an exception will be thrown.</remarks>
        public static DatabaseParameterCollection Param(this DatabaseParameterCollection dpc, string name, TableValuedParameter tvp)
        {
            (dpc ?? throw new ArgumentNullException(nameof(dpc))).AddTableValuedParameter(name, tvp);
            return dpc;
        }

        /// <summary>
        /// Uses additional <b>SQL Server</b> specific schema inference (see <see cref="Schema.DbSchemaArgs.Additional"/>).
        /// </summary>
        /// <param name="args">The <see cref="Schema.DbSchemaArgs"/>.</param>
        /// <param name="additional">Function to add additional logic.</param>
        /// <remarks>The <see cref="Schema.DbSchemaArgs"/> to support fluent-style method-chaining.</remarks>
        public static Schema.DbSchemaArgs UseSqlServerAdditional(this Schema.DbSchemaArgs args, Func<IDatabase, List<Schema.DbTable>, Task>? additional = null)
        {
            args.Additional = async (db, tables) =>
            {
                using var sr4 = StreamLocator.GetResourcesStreamReader("SelectTableIdentityColumns.sql")!;
                await db.SqlStatement(await sr4.ReadToEndAsync().ConfigureAwait(false)).SelectAsync(new DatabaseRecordMapper(dr =>
                {
                    var t = tables.Single(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                    var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                    c.IsIdentity = true;
                    c.IdentitySeed = 1;
                    c.IdentityIncrement = 1;
                })).ConfigureAwait(false);

                using var sr5 = StreamLocator.GetResourcesStreamReader("SelectTableAlwaysGeneratedColumns.sql")!;
                await db.SqlStatement(await sr5.ReadToEndAsync().ConfigureAwait(false)).SelectAsync(new DatabaseRecordMapper(dr =>
                {
                    var t = tables.Single(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                    var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                    t.Columns.Remove(c);
                })).ConfigureAwait(false);

                using var sr6 = StreamLocator.GetResourcesStreamReader("SelectTableGeneratedColumns.sql")!;
                await db.SqlStatement(await sr6.ReadToEndAsync().ConfigureAwait(false)).SelectAsync(new DatabaseRecordMapper(dr =>
                {
                    var t = tables.Single(x => x.Schema == dr.GetValue<string>("TABLE_SCHEMA") && x.Name == dr.GetValue<string>("TABLE_NAME"));
                    var c = t.Columns.Single(x => x.Name == dr.GetValue<string>("COLUMN_NAME"));
                    c.IsComputed = true;
                })).ConfigureAwait(false);

                if (additional != null)
                    await additional(db, tables).ConfigureAwait(false);
            };

            return args;
        }
    }
}