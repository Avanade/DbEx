// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System.Text.Json.Serialization;

namespace DbEx.Migration.Data
{
    /// <summary>
    /// Represents the data configuration.
    /// </summary>
    public class DataConfig
    {
        /// <summary>
        /// Gets or sets the pre-condition SQL.
        /// </summary>
        [JsonPropertyName("preConditionSql")]
        public string? PreConditionSql { get; set; }

        /// <summary>
        /// Gets or sets the pre-SQL.
        /// </summary>
        [JsonPropertyName("preSql")]
        public string? PreSql { get; set; }

        /// <summary>
        /// Gets or sets the post-SQL.
        /// </summary>
        [JsonPropertyName("postSql")]
        public string? PostSql { get; set; }
    }
}