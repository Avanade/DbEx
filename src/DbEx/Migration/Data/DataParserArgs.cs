// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Schema;
using System;
using System.Collections.Generic;

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
        /// Gets or sets the reference data predicate used to determine whether a <see cref="DbTableSchema"/> is considered a reference data table (sets <see cref="DbTableSchema.IsRefData"/>).
        /// </summary>
        public Func<DbTableSchema, bool>? RefDataPredicate { get; set; }

        /// <summary>
        /// Gets or sets the list of reference data column defaults.
        /// </summary>
        /// <remarks>The list should contain the column name and function that returns the default value (the input to the function is the corresponding row count as specified).</remarks>
        public List<(string Name, Func<int, object?> Value)>? RefDataColumnDefaults { get; } = new List<(string Name, Func<int, object?> Value)>();

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> format.
        /// </summary>
        /// <remarks>Defaults to '<c>yyyy-MM-ddTHH:mm:ss.fffffff</c>'.</remarks>
        public string DateTimeFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>
        /// Gets the runtime parameters.
        /// </summary>
        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();
    }
}