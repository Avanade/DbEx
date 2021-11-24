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

    /// <summary>
    /// Provides a runtime generic <see cref="IDatabaseMapper{T}"/>.
    /// </summary>
    /// <typeparam name="T">The resulting <see cref="Type"/>.</typeparam>
    public class DatabaseRecordMapper2<T> : IDatabaseMapper<T>
    {
        private readonly Func<DatabaseRecord, T> _record;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRecordMapper2{T}"/> class.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/> function.</param>
        public DatabaseRecordMapper2(Func<DatabaseRecord, T> record) => _record = record ?? throw new ArgumentNullException(nameof(record));

        /// <inheritdoc/>
        public T MapFromDb(DatabaseRecord record) => _record(record);
    }
}