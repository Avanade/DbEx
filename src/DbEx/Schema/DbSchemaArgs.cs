// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbEx.Schema
{
    /// <summary>
    /// Provides the <see cref="IDatabase.SelectSchemaAsync(DbSchemaArgs?)"/> arguments.
    /// </summary>
    public class DbSchemaArgs
    {
        /// <summary>
        /// Gets or sets the reference data predicate used to determine whether a <see cref="DbTable"/> is considered a reference data table (sets <see cref="DbTable.IsRefData"/>).
        /// </summary>
        /// <remarks>The parameter passed is the <see cref="DbTable"/> to be validated as reference data. A result of <c>true</c> indicates that the table is considered a reference
        /// data table; otherwise, <c>false</c>. The default is that all tables are <i>not</i> considered reference data; in that it will result in <c>false</c>.</remarks>
        public Func<DbTable, bool> RefDataPredicate { get; set; } = _ => false;

        /// <summary>
        /// Gets or sets the <i>additional</i> functions that enables further processing on the <see cref="DbTable"/> list. 
        /// </summary>
        public Func<IDatabase, List<DbTable>, Task>? Additional { get; set; }
    }
}