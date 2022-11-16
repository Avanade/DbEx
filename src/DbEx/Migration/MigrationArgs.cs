// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

namespace DbEx.Migration
{
    /// <summary>
    /// Provides the <see cref="DatabaseMigrationBase"/> arguments.
    /// </summary>
    public class MigrationArgs : MigrationArgsBase<MigrationArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationArgs"/> class.
        /// </summary>
        public MigrationArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationArgs"/> class.
        /// </summary>
        /// <param name="migrationCommand">The <see cref="MigrationCommand"/>.</param>
        /// <param name="connectionString">The optional connection string.</param>
        public MigrationArgs(MigrationCommand migrationCommand, string? connectionString = null)
        {
            MigrationCommand = migrationCommand;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Copy and replace from <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgs"/> to copy from.</param>
        public void CopyFrom(MigrationArgs args) => base.CopyFrom(args);
    }
}