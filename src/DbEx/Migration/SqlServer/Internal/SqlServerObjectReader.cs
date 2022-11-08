// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DbEx.Migration.SqlServer.Internal
{
    /// <summary>
    /// Represents a <i>very basic</i> SQL Server '<c>CREATE</c>' object lexer to determine the corresponding <see cref="Type"/>, <see cref="Schema"/> and <see cref="Name"/> being created.
    /// </summary>
    public class SqlServerObjectReader
    {
        private readonly TextReader _tr;
        private readonly string[] _knownSchemaObjectTypes;
        private readonly string[] _schemaOrder;
        private readonly List<string> _tokens = new();
        private string? _sql;

        /// <summary>
        /// Reads and parses the SQL <see cref="string"/>.
        /// </summary>
        /// <param name="scriptName">The originating script name.</param>
        /// <param name="sql">The SQL <see cref="string"/>.</param>
        /// <param name="knownSchemaObjectTypes">The list of known schema object types.</param>
        /// <param name="schemaOrder">The schema priority list (used to specify schema precedence; otherwise equal last).</param>
        /// <returns>A <see cref="SqlServerObjectReader"/>.</returns>
        public static SqlServerObjectReader Read(string scriptName, string sql, string[] knownSchemaObjectTypes, string[] schemaOrder)
        {
            using var sr = new StringReader(sql);
            return Read(scriptName, sr, knownSchemaObjectTypes, schemaOrder);
        }

        /// <summary>
        /// Reads and parses the SQL <see cref="Stream"/>.
        /// </summary>
        /// <param name="scriptName">The originating script name.</param>
        /// <param name="s">The SQL <see cref="Stream"/>.</param>
        /// <param name="knownSchemaObjectTypes">The list of known schema object types.</param>
        /// <param name="schemaOrder">The schema priority list (used to specify schema precedence; otherwise equal last).</param>
        /// <returns>A <see cref="SqlServerObjectReader"/>.</returns>
        public static SqlServerObjectReader Read(string scriptName, Stream s, string[] knownSchemaObjectTypes, string[] schemaOrder)
        {
            using var sr = new StreamReader(s);
            return Read(scriptName, sr, knownSchemaObjectTypes, schemaOrder);
        }

        /// <summary>
        /// Reads and parses the SQL <see cref="TextReader"/>.
        /// </summary>
        /// <param name="scriptName">The originating script name.</param>
        /// <param name="tr">The SQL <see cref="TextReader"/>.</param>
        /// <param name="knownSchemaObjectTypes">The list of known schema object types.</param>
        /// <param name="schemaOrder">The schema priority list (used to specify schema precedence; otherwise equal last).</param>
        /// <returns>A <see cref="SqlServerObjectReader"/>.</returns>
        public static SqlServerObjectReader Read(string scriptName, TextReader tr, string[] knownSchemaObjectTypes, string[] schemaOrder) => new(scriptName, tr, knownSchemaObjectTypes, schemaOrder);

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerObjectReader"/> class.
        /// </summary>
        private SqlServerObjectReader(string scriptName, TextReader tr, string[] knownSchemaObjectTypes, string[] schemaOrder)
        {
            ScriptName = scriptName ?? throw new ArgumentNullException(nameof(scriptName));
            _tr = tr ?? throw new ArgumentNullException(nameof(tr));
            _knownSchemaObjectTypes = knownSchemaObjectTypes;
            _schemaOrder = schemaOrder;

            // Always default dbo first if nothing specified.
            if (_schemaOrder.Length == 0)
                _schemaOrder = new string[] { "dbo" };

            Parse();

            TypeOrder = GetOperationOrder();
            ErrorMessage = CreateErrorMessage();
            if (!IsValid)
                return;

            var parts = SqlObjectName!.Split('.');
            if (parts.Length == 1)
            {
                Schema = "dbo";
                Name = parts[0].Replace('[', ' ').Replace(']', ' ').Trim();
            }
            else if (parts.Length == 2)
            {
                Schema = parts[0].Replace('[', ' ').Replace(']', ' ').Trim();
                Name = parts[1].Replace('[', ' ').Replace(']', ' ').Trim();
            }
            else
                ErrorMessage = $"The SQL object name is not valid.";

            var so = Array.FindIndex(_schemaOrder, x => string.Compare(x, Schema, StringComparison.InvariantCultureIgnoreCase) == 0);
            SchemaOrder = so < 0 ? _schemaOrder.Length : so;
        }

        /// <summary>
        /// Read file and parse out the primary tokens.
        /// </summary>
        private void Parse()
        {
            _sql = _tr.ReadToEnd();

            int start = -1;
            string? line;
            using var sr = new StringReader(_sql);

            var stmts = SqlServerMigrator.SplitAndCleanSql(sr);
            if (stmts.Count == 0)
                return;

            if (stmts.Count > 1)
            {
                ErrorMessage = "The SQL contains more than a single statement.";
                return;
            }

            using var tr = new StringReader(stmts[0].CleanSql);
            while ((line = tr.ReadLine()) is not null)
            {
                // Parse out the token(s).
                var col = 0;
                for (; col < line.Length; col++)
                {
                    if (char.IsWhiteSpace(line[col]) || new char[] { ',', ';', '(', ')', '{', '}' }.Contains(line[col]))
                    {
                        if (start >= 0)
                        {
                            _tokens.Add(line[start..col]);
                            start = -1;

                            if (_tokens.Count > 2)
                                return;
                        }
                    }
                    else if (start < 0)
                        start = col;
                }

                if (start >= 0)
                {
                    _tokens.Add(line[start..col]);
                    start = -1;
                }
            }
        }

        /// <summary>
        /// Gets the originating script name.
        /// </summary>
        public string ScriptName { get; }

        /// <summary>
        /// Indicates whether the SQL Object is valid.
        /// </summary>
        public bool IsValid => ErrorMessage == null;

        /// <summary>
        /// Gets the error message where not valid (see <see cref="IsValid"/>).
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the primary SQL command (first token).
        /// </summary>
        private string? SqlStatement => _tokens.Count < 1 ? null : _tokens[0];

        /// <summary>
        /// Gets the underlying SQL object type (second token).
        /// </summary>
        private string? SqlObjectType => _tokens.Count < 2 ? null : _tokens[1];

        /// <summary>
        /// Gets the underlying SQL object name (third token).
        /// </summary>
        private string? SqlObjectName => _tokens.Count < 3 ? null : _tokens[2];

        /// <summary>
        /// Gets the SQL object type.
        /// </summary>
        public string? Type { get; private set; }

        /// <summary>
        /// Gets the SQL object schema.
        /// </summary>
        public string? Schema { get; private set; }

        /// <summary>
        /// Gets the schema order.
        /// </summary>
        public int SchemaOrder { get; private set; }

        /// <summary>
        /// Gets the SQL object name.
        /// </summary>
        public string? Name { get; private set; }

        /// <summary>
        /// Gets the SQL object type order of precedence.
        /// </summary>
        public int TypeOrder { get; private set; }

        /// <summary>
        /// Gets the underlying SQL <see cref="string"/>.
        /// </summary>
        /// <returns>The SQL <see cref="string"/>.</returns>
        public string GetSql() => _sql!;

        /// <summary>
        /// Create the error message where not valid.
        /// </summary>
        private string? CreateErrorMessage()
        {
            if (SqlStatement == null)
                return "The SQL statement could not be determined; expecting a `CREATE` statement.";
            else if (string.Compare(SqlStatement, "create", StringComparison.InvariantCultureIgnoreCase) != 0)
                return $"The SQL statement must be a `CREATE`; found '{SqlStatement}'.";

            if (Type == null)
                return "The SQL object type could not be determined.";
            else if (TypeOrder < 0)
                return $"The SQL object type '{Type}' is not supported; this should be added as a Script.";

            if (SqlObjectName == null)
                return "The SQL object name could not be determined.";

            return null;
        }

        /// <summary>
        /// Gets the corresponding database operation order.
        /// </summary>
        private int GetOperationOrder()
        {
            if (SqlObjectType == null)
                return -1;

            Type = _knownSchemaObjectTypes.Where(x => string.Compare(x, SqlObjectType, StringComparison.InvariantCultureIgnoreCase) == 0).SingleOrDefault();
            return Type == null ? -1 : Array.IndexOf(_knownSchemaObjectTypes, Type);
        }
    }
}