// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;

namespace DbEx.Console
{
    /// <summary>
    /// Provides the <see cref="MigratorConsoleBase"/> arguments.
    /// </summary>
    public class MigratorConsoleArgs : MigratorConsoleArgsBase<MigratorConsoleArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleArgs"/> class.
        /// </summary>
        public MigratorConsoleArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigratorConsoleArgs"/> class.
        /// </summary>
        /// <param name="migrationCommand">The <see cref="Migration.MigrationCommand"/>.</param>
        /// <param name="connectionString">The optional connection string.</param>
        public MigratorConsoleArgs(MigrationCommand migrationCommand, string? connectionString = null)
        {
            MigrationCommand = migrationCommand;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Copy and replace from <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigratorConsoleArgs"/> to copy from.</param>
        public void CopyFrom(MigratorConsoleArgs args) => base.CopyFrom(args);
    }
}