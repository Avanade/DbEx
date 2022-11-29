// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx.Entities;
using CoreEx.RefData;
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
        /// Gets or sets the suffix of the identifier column where not fully specified.
        /// </summary>
        /// <remarks>Where matching columns and the specified column is not found, then the suffix will be appended to the specified column name and an additional match will be performed.
        /// <para>Defaults to <see cref="DatabaseSchemaConfig.IdColumnNameSuffix"/> where not specified (i.e. <c>null</c>).</para></remarks>
        public string? IdColumnNameSuffix { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.CreatedDate"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.CreatedDateColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? CreatedDateColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.CreatedBy"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.CreatedByColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? CreatedByColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.UpdatedDate"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.UpdatedDateColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? UpdatedDateColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IChangeLogAudit.UpdatedBy"/> column (where it exists).
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.UpdatedByColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? UpdatedByColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IReferenceData.Code"/> column.
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RefDataCodeColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RefDataCodeColumnName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="IReferenceData.Text"/> column.
        /// </summary>
        /// <remarks>Defaults to <see cref="DatabaseSchemaConfig.RefDataTextColumnName"/> where not specified (i.e. <c>null</c>).</remarks>
        public string? RefDataTextColumnName { get; set; }

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
        /// Gets or sets the <see cref="DbTableSchema"/> updater.
        /// </summary>
        /// <remarks>This is invoked offering an opportunity to further update (manipulate) the <see cref="DbTableSchema"/> selected from the database using the <see cref="DatabaseExtensions.SelectSchemaAsync"/>.</remarks>
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