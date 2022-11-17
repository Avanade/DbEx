﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.DbSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents the <see cref="DataParser"/> arguments.
    /// </summary>
    public class DataParserArgs
    {
        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        /// <remarks>Defaults to '<c><see cref="Environment.UserDomainName"/>/<see cref="Environment.UserName"/></c>'.</remarks>
        public string UserName { get; set; } = Environment.UserDomainName == null ? Environment.UserName : $"{Environment.UserDomainName}\\{Environment.UserName}";

        /// <summary>
        /// Gets or sets the current <see cref="DateTime"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="DateTime.UtcNow"/>.</remarks>
        public DateTime DateTimeNow { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the name of the <i>CreatedDate</i> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to '<c>CreatedDate</c>'.</remarks>
        public string CreatedDateColumnName { get; set; } = "CreatedDate";

        /// <summary>
        /// Gets or sets the name of the <i>CreatedBy</i> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to '<c>CreatedBy</c>'.</remarks>
        public string CreatedByColumnName { get; set; } = "CreatedBy";

        /// <summary>
        /// Gets or sets the name of the <i>UpdatedDate</i> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to '<c>UpdatedDate</c>'.</remarks>
        public string UpdatedDateColumnName { get; set; } = "UpdatedDate";

        /// <summary>
        /// Gets or sets the name of the <i>UpdatedBy</i> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to '<c>UpdatedBy</c>'.</remarks>
        public string UpdatedByColumnName { get; set; } = "UpdatedBy";

        /// <summary>
        /// Gets or sets the reference data <i>Code</i> column (where it is supported).
        /// </summary>
        /// <remarks>Defaults to '<c>Code</c>'.</remarks>
        public string RefDataCodeColumnName { get; set; } = "Code";

        /// <summary>
        /// Gets or sets the reference data <i>Text</i> column (where it is supported).
        /// </summary>
        /// <remarks>Defaults to '<c>Text</c>'.</remarks>
        public string RefDataTextColumnName { get; set; } = "Text";

        /// <summary>
        /// Gets or sets the reference data alternate schema name; used where attempting to infer reference data relationship after using same schema as the first option.
        /// </summary>
        /// <remarks>Defaults to '<c>Ref</c>'.</remarks>
        public string? RefDataAlternateSchema { get; set; } = "Ref";

        /// <summary>
        /// Gets or sets the <see cref="IIdentifierGenerator"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="GuidIdentifierGenerator"/>.</remarks>
        public IIdentifierGenerator IdentifierGenerator { get; set; } = new GuidIdentifierGenerator();

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> format.
        /// </summary>
        /// <remarks>Defaults to '<c>yyyy-MM-ddTHH:mm:ss.fffffff</c>'.</remarks>
        public string DateTimeFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>
        /// Gets or sets the reference data column defaults dictionary.
        /// </summary>
        /// <remarks>The list should contain the column name and function that returns the default value (the input to the function is the corresponding row count as specified).</remarks>
        public Dictionary<string, Func<int, object?>> RefDataColumnDefaults { get; } = new Dictionary<string, Func<int, object?>>();

        /// <summary>
        /// Gets the runtime parameters.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();

        /// <summary>
        /// Creates a predicate that uses the <see cref="RefDataCodeColumnName"/> and <see cref="RefDataTextColumnName"/> as the means to identify tables as being <see cref="DbTableSchema.IsRefData"/>.
        /// </summary>
        /// <returns>The predicate.</returns>
        /// <remarks>Both the <see cref="RefDataCodeColumnName"/> and <see cref="RefDataTextColumnName"/> must exist, must not be <see cref="DbColumnSchema.IsPrimaryKey"/>, and have a corresponding <see cref="DbColumnSchema.DotNetType"/> of '<c>string</c>'.</remarks>
        public Func<DbTableSchema, bool>? CreateRefDataPredicate()
            => t => t.Columns.Any(c => c.Name == RefDataCodeColumnName && !c.IsPrimaryKey && c.DotNetType == "string") && t.Columns.Any(c => c.Name == RefDataTextColumnName && !c.IsPrimaryKey && c.DotNetType == "string");

        /// <summary>
        /// Gets or sets the <see cref="DbTableSchema"/> updater.
        /// </summary>
        /// <remarks>This is invoked offering an opportunity to further update (manipulate) the <see cref="DbTableSchema"/> selected from the database using the <see cref="DatabaseExtensions.SelectSchemaAsync(CoreEx.Database.IDatabase, Func{DbTableSchema, bool}?, System.Threading.CancellationToken)"/>.</remarks>
        public Func<IEnumerable<DbTableSchema>, CancellationToken, Task<IEnumerable<DbTableSchema>>>? DbSchemaUpdaterAsync { get; set; }

        /// <summary>
        /// Copy and replace from <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="DataParserArgs"/> to copy from.</param>
        public void CopyFrom(DataParserArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            UserName = args.UserName;
            DateTimeNow = args.DateTimeNow;
            CreatedDateColumnName = args.CreatedDateColumnName;
            CreatedByColumnName = args.CreatedByColumnName;
            UpdatedDateColumnName = args.UpdatedDateColumnName;
            UpdatedByColumnName = args.UpdatedByColumnName;
            RefDataCodeColumnName = args.RefDataCodeColumnName;
            RefDataTextColumnName = args.RefDataTextColumnName;
            IdentifierGenerator = args.IdentifierGenerator;
            DateTimeFormat = args.DateTimeFormat;
            DbSchemaUpdaterAsync = args.DbSchemaUpdaterAsync;
            RefDataColumnDefaults.Clear();
            args.RefDataColumnDefaults.ForEach(x => RefDataColumnDefaults.Add(x.Key, x.Value));
            Parameters.Clear();
            args.Parameters.ForEach(x => Parameters.Add(x.Key, x.Value));
        }
    }
}