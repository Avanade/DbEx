// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using DbUp.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbEx.Postgres.Migration
{
    /// <summary>
    /// Provides the PostgreSQL database schema script functionality.
    /// </summary>
    public class PostgresSchemaScript : DatabaseSchemaScriptBase
    {
        /// <summary>
        /// Creates the <see cref="PostgresSchemaScript"/> from the <see cref="DatabaseMigrationScript"/>.
        /// </summary>
        /// <param name="migrationScript">The <see cref="DatabaseMigrationScript"/>.</param>
        /// <returns>The <see cref="PostgresSchemaScript"/>.</returns>
        public static PostgresSchemaScript Create(DatabaseMigrationScript migrationScript)
        {
            var script = new PostgresSchemaScript(migrationScript);

            using var sr = script.MigrationScript.GetStreamReader();
            var sql = sr.ReadToEnd();
            var tokens = new SqlCommandTokenizer(sql).ReadAllTokens();

            for (int i = 0; i < tokens.Length; i++)
            {
                if (string.Compare(tokens[i], "create", StringComparison.OrdinalIgnoreCase) != 0)
                    continue;

                if (i + 4 < tokens.Length)
                {
                    if (string.Compare(tokens[i + 1], "or", StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(tokens[i + 2], "replace", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        i =+ 2;
                        script.SupportsReplace = true;
                    }

                    script.Type = tokens[i + 1];
                    script.FullyQualifiedName = tokens[i + 2];

                    var index = script.FullyQualifiedName.IndexOf('.');
                    if (index < 0)
                    {
                        script.Schema = migrationScript.DatabaseMigration.SchemaConfig.DefaultSchema;
                        script.Name = script.FullyQualifiedName;
                    }
                    else
                    {
                        script.Schema = script.FullyQualifiedName[..index];
                        script.Name = script.FullyQualifiedName[(index + 1)..];
                    }

                    return script;
                }
            }

            script.ErrorMessage = "The SQL statement must be a valid 'CREATE' statement.";
            return script;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresSchemaScript"/> class.
        /// </summary>
        /// <param name="migrationScript">The parent <see cref="DatabaseMigrationScript"/>.</param>
        private PostgresSchemaScript(DatabaseMigrationScript migrationScript) : base(migrationScript, "\"", "\"") { }

        /// <inheritdoc/>
        public override string SqlDropStatement => $"DROP {Type.ToUpperInvariant()} IF EXISTS \"{Schema}\".\"{Name}\"";

        /// <inheritdoc/>
        public override string SqlCreateStatement => $"CREATE {(SupportsReplace ? "OR REPLACE " : "")}{Type.ToUpperInvariant()} \"{Schema}\".\"{Name}\"";

        private class SqlCommandTokenizer(string sqlText) : SqlCommandReader(sqlText)
        {
            private readonly char[] delimiters = ['(', ')', ';', ',', '='];

            public string[] ReadAllTokens()
            {
                var words = new List<string>();
                var sb = new StringBuilder();

                while (!HasReachedEnd)
                {
                    ReadCharacter += (type, c) =>
                    {
                        switch (type)
                        {
                            case CharacterType.Command:
                                if (char.IsWhiteSpace(c))
                                {
                                    if (sb.Length > 0)
                                        words.Add(sb.ToString());

                                    sb.Clear();
                                    break;
                                }
                                else if (delimiters.Contains(c))
                                {
                                    if (sb.Length > 0)
                                        words.Add(sb.ToString());

                                    sb.Clear();
                                }

                                sb.Append(c);
                                break;

                            case CharacterType.BracketedText:
                            case CharacterType.QuotedString:
                                sb.Append(c);
                                break;

                            case CharacterType.SlashStarComment:
                            case CharacterType.DashComment:
                            case CharacterType.CustomStatement:
                            case CharacterType.Delimiter:
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(type), type, null);
                        }
                    };

                    Parse();
                }

                if (sb.Length > 0)
                    words.Add(sb.ToString());

                return [.. words];
            }
        }
    }
}