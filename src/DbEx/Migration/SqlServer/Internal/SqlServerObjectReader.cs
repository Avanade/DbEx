// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        private readonly List<string?> _lines = new();
        private readonly List<Token> _tokens = new();

        /// <summary>
        /// Represents the token characteristics.
        /// </summary>
        internal class Token
        {
            /// <summary>
            /// Gets or sets the line.
            /// </summary>
            public int Line { get; set; }

            /// <summary>
            /// Gets or sets the column.
            /// </summary>
            public int Column { get; set; }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            public string? Value { get; set; }
        }

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

            var parts = SqlObjectName!.Value!.Split('.');
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
            Token? token = null;
            bool inComment = false;

            while (true)
            {
                var txt = _tr.ReadLine();
                if (txt == null)
                    break;

                _lines.Add(txt.TrimEnd());

                // Stop parsing after 3 tokens; howver, keep filling the statement list.
                if (_tokens.Count >= 3)
                    continue;

                txt = txt.Trim();
                int ci = 0;

                // Remove /* */ comments
                if (inComment)
                {
                    ci = txt.IndexOf("*/");
                    if (ci < 0)
                        continue;

                    txt = txt[(ci + 2)..].Trim();
                    inComment = false;
                }

                ci = txt.IndexOf("/*");
                if (ci >= 0)
                {
                    var ci2 = txt.IndexOf("*/");
                    if (ci2 >= 0)
                        txt = (txt[0..ci] + txt[(ci2 + 2)..]).Trim();
                    else
                    {
                        txt = txt[0..ci].Trim();
                        inComment = true;
                    }
                }

                // Remove -- comments.
                ci = txt.IndexOf("--", StringComparison.InvariantCulture);
                if (ci >= 0)
                    txt = txt[..ci].Trim();

                // Parse out the token(s).
                var col = 0;
                for (; col < txt.Length; col++)
                {
                    if (char.IsWhiteSpace(txt[col]) || new char[] { ',', ';', '(', ')', '{', '}' }.Contains(txt[col]))
                    {
                        if (token != null)
                        {
                            token.Value = txt[token.Column..col];
                            _tokens.Add(token);
                            token = null;
                        }
                    }
                    else if (token == null)
                    {
                        token = new Token { Line = _lines.Count - 1, Column = col };
                    }
                }

                if (token != null)
                {
                    token.Value = txt[token.Column..col];
                    _tokens.Add(token);
                    token = null;
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
        private Token? SqlStatement => _tokens.Count < 1 ? null : _tokens[0];

        /// <summary>
        /// Gets the underlying SQL object type (second token).
        /// </summary>
        private Token? SqlObjectType => _tokens.Count < 2 ? null : _tokens[1];

        /// <summary>
        /// Gets the underlying SQL object name (third token).
        /// </summary>
        private Token? SqlObjectName => _tokens.Count < 3 ? null : _tokens[2];

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
        public string GetSql()
        {
            var sb = new StringBuilder();
            _lines.ForEach((l) => sb.AppendLine(l));
            return sb.ToString();
        }

        /// <summary>
        /// Create the error message where not valid.
        /// </summary>
        private string? CreateErrorMessage()
        {
            if (SqlStatement == null)
                return "The SQL statement could not be determined; expecting a `CREATE` statement.";
            else if (string.Compare(SqlStatement.Value, "create", StringComparison.InvariantCultureIgnoreCase) != 0)
                return $"The SQL statement must be a `CREATE`; found '{SqlStatement.Value}'.";

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

            Type = _knownSchemaObjectTypes.Where(x => string.Compare(x, SqlObjectType.Value, StringComparison.InvariantCultureIgnoreCase) == 0).SingleOrDefault();
            return Type == null ? -1 : Array.IndexOf(_knownSchemaObjectTypes, Type);
        }
    }
}