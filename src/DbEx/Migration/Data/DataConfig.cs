// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Respresents the data configuration.
    /// </summary>
    public class DataConfig
    {
        /// <summary>
        /// Gets or sets the pre-condition SQL.
        /// </summary>
        public string? PreConditionSql { get; set; }

        /// <summary>
        /// Gets or sets the pre-SQL.
        /// </summary>
        public string? PreSql { get; set; }

        /// <summary>
        /// Gets or sets the post-SQL.
        /// </summary>
        public string? PostSql { get; set; }
    }
}