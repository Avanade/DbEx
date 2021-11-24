// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbEx.Schema
{
    /// <summary>
    /// Represents the Database <b>Table</b> schema definition.
    /// </summary>
    public class DbTable
    {
        /// <summary>
        /// The <see cref="Regex"/> expression pattern for splitting strings into words.
        /// </summary>
        public const string WordSplitPattern = "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))";

        private string? _name;

        /// <summary>
        /// Create an alias from the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The corresponding alias.</returns>
        /// <remarks>Converts the name into sentence case and takes first character from each word and converts to lowercase; e.g. '<c>SalesOrder</c>' will result in an alias of '<c>so</c>'.</remarks>
        public static string CreateAlias(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var s = Regex.Replace(name, WordSplitPattern, "$1 "); // Split the string into words.
            return new string(s.Replace(" ", " ").Replace("_", " ").Replace("-", " ").Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Substring(0, 1).ToLower(System.Globalization.CultureInfo.InvariantCulture).ToCharArray()[0]).ToArray());
        }

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string? Name
        {
            get { return _name; }

            set
            {
                _name = value;
                if (!string.IsNullOrEmpty(_name) && string.IsNullOrEmpty(Alias))
                    Alias = CreateAlias(_name);
            }
        }

        /// <summary>
        /// Gets or sets the schema.
        /// </summary>
        public string? Schema { get; set; }

        /// <summary>
        /// Gets or sets the alias (automatically updated when the <see cref="Name"/> is set and the current alias value is <c>null</c>).
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Gets the fully qualified name '<c>[schema].[table]</c>' name.
        /// </summary>
        public string? QualifiedName => $"[{Schema}].[{Name}]";

        /// <summary>
        /// Indicates whether the Table is actually a View.
        /// </summary>
        public bool IsAView { get; set; }

        /// <summary>
        /// Indicates whether the Table is considered reference data.
        /// </summary>
        public bool IsRefData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbColumn"/> list.
        /// </summary>
        public List<DbColumn> Columns { get; private set; } = new List<DbColumn>();

        /// <summary>
        /// Gets the primary key <see cref="DbColumn"/> list.
        /// </summary>
        public List<DbColumn> PrimaryKeyColumns => Columns?.Where(x => x.IsPrimaryKey).ToList() ?? new List<DbColumn>();
    }
}