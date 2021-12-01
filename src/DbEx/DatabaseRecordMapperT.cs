// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;

namespace DbEx
{
    /// <summary>
    /// Provides a runtime generic <see cref="IDatabaseMapper{T}"/>.
    /// </summary>
    /// <typeparam name="T">The resulting <see cref="Type"/>.</typeparam>
    public class DatabaseRecordMapper<T> : IDatabaseMapper<T>
    {
        private readonly Func<DatabaseRecord, T> _record;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRecordMapper{T}"/> class.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/> function.</param>
        public DatabaseRecordMapper(Func<DatabaseRecord, T> record) => _record = record ?? throw new ArgumentNullException(nameof(record));

        /// <inheritdoc/>
        public T MapFromDb(DatabaseRecord record) => _record(record);
    }
}