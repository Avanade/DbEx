// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx
{
    /// <summary>
    /// Provides a per <see cref="DatabaseRecord"/> mapper that simulates a <see cref="IDatabaseMapper{T}.MapFromDb(DatabaseRecord)"/> by invoking the action passed via the <see cref="DatabaseRecordMapper(Action{DatabaseRecord})"/>.
    /// </summary>
    public class DatabaseRecordMapper : IDatabaseMapper<object?>
    {
        private readonly Action<DatabaseRecord> _record;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRecordMapper"/> class.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/> action.</param>
        public DatabaseRecordMapper(Action<DatabaseRecord> record) => _record = record ?? throw new ArgumentNullException(nameof(record));
        
        /// <inheritdoc/>
        object? IDatabaseMapper<object?>.MapFromDb(DatabaseRecord record)
        {
            _record(record);
            return null;
        }
    }
}