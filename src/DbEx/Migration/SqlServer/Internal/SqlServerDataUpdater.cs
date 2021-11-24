// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbEx.Migration.SqlServer.Internal
{
    public sealed class SqlServerDataUpdater
    {
        private readonly IDatabase _database;
        private readonly List<Schema.DbTable> _tables;

        public Task<SqlServerDataUpdater> CreateAsync(IDatabase database)
        {
            var tables = (database ?? throw new ArgumentNullException(nameof(database))).SelectSchemaAsync(new Schema.DbSchemaArgs {  } )
        }

        private SqlServerDataUpdater(IDatabase database, List<Schema.DbTable> tables)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _tables = tables ?? throw new ArgumentNullException(nameof(tables));
        }

    }
}
