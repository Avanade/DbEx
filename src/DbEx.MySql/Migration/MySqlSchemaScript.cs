// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using DbUp.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbEx.MySql.Migration
{
    /// <summary>
    /// Provides the MySQL database schema script functionality.
    /// </summary>
    public class MySqlSchemaScript : DatabaseSchemaScriptBase
    {
        /// <summary>
        /// Creates the <see cref="MySqlSchemaScript"/> from the <see cref="DatabaseMigrationScript"/>.
        /// </summary>
        /// <param name="migrationScript">The <see cref="DatabaseMigrationScript"/>.</param>
        /// <returns>The <see cref="MySqlSchemaScript"/>.</returns>
        public static MySqlSchemaScript Create(DatabaseMigrationScript migrationScript)
        {
            var script = new MySqlSchemaScript(migrationScript);

            using var sr = script.MigrationScript.GetStreamReader();
            var sql = sr.ReadToEnd();
            var tokens = new SqlCommandTokenizer(sql).ReadAllTokens();

            for (int i = 0; i < tokens.Length; i++)
            {
                if (string.Compare(tokens[i], "create", StringComparison.OrdinalIgnoreCase) != 0)
                    continue;

                if (i + 2 < tokens.Length)
                {
                    script.Type = tokens[i + 1];
                    script.FullyQualifiedName = tokens[i + 2];
                    script.Name = script.FullyQualifiedName;
                    return script;
                }
            }

            script.ErrorMessage = "The SQL statement must be a valid 'CREATE' statement.";
            return script;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlSchemaScript"/> class.
        /// </summary>
        /// <param name="migrationScript">The parent <see cref="DatabaseMigrationScript"/>.</param>
        private MySqlSchemaScript(DatabaseMigrationScript migrationScript) : base(migrationScript, "`", "`") { }

        /// <inheritdoc/>
        public override string SqlDropStatement => $"DROP {Type.ToUpperInvariant()} IF EXISTS `{Name}`";

        /// <inheritdoc/>
        public override string SqlCreateStatement => $"CREATE {Type.ToUpperInvariant()} `{Name}`";

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